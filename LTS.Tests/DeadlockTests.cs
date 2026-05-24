using LTS.Application.Services;
using LTS.Common.Constants;
using LTS.Common.Interfaces;
using LTS.Common.Models;
using Moq;
using Xunit;

namespace LTS.Tests;

public class DeadlockTests
{
    // ── HELPERS ───────────────────────────────────────────────────

    private Mock<IProcessLocationRepository> _locationRepo = new();
    private Mock<ILotRepository> _lotRepo = new();
    private Mock<ILotRouteRepository> _routeRepo = new();
    private Mock<ICarrierRepository> _carrierRepo = new();
    private Mock<ILogRepository> _logRepo = new();

    private ProcessLocationService CreateService()
    {
        _logRepo.Setup(r => r.AddAsync(It.IsAny<ApplicationLog>()))
                .Returns(Task.CompletedTask);

        var logService = new LogService(_logRepo.Object);

        return new ProcessLocationService(
            _locationRepo.Object,
            _lotRepo.Object,
            _routeRepo.Object,
            _carrierRepo.Object,
            logService);
    }

    private Lot MakeLot(
        int id,
        string status = LotStatus.Processing,
        int carrierId = 1,
        int? currentLocationId = null,
        DateTime? startedOn = null)
        => new Lot
        {
            LotId = id,
            LotName = $"LOT-00{id}",
            LotStatus = status,
            CarrierId = carrierId,
            CurrentProcessLocationId = currentLocationId,
            StartedOn = startedOn ?? DateTime.Now.AddMinutes(-id)
        };

    private ProcessLocation MakeLocation(
        int id,
        string status = LocationStatus.Available,
        int? currentLotId = null)
        => new ProcessLocation
        {
            ProcessLocationId = id,
            Name = $"Location {id}",
            Status = status,
            CurrentLotId = currentLotId
        };

    private LotRoute MakeStep(
        int lotRouteId,
        int lotId,
        int locationId,
        int stepOrder,
        bool isCompleted = false)
        => new LotRoute
        {
            LotRouteId = lotRouteId,
            LotId = lotId,
            ProcessLocationId = locationId,
            StepOrder = stepOrder,
            IsCompleted = isCompleted
        };

    // shared setup for deadlock scenario
    private (Lot lot1, Lot lot2,
             ProcessLocation loc2, ProcessLocation loc3,
             List<LotRoute> lot1Route, List<LotRoute> lot2Route)
        SetupDeadlock(
            DateTime? lot1Start = null,
            DateTime? lot2Start = null)
    {
        var lot1 = MakeLot(1, currentLocationId: 2,
            startedOn: lot1Start ?? DateTime.Now.AddMinutes(-10));
        var lot2 = MakeLot(2, currentLocationId: 3,
            startedOn: lot2Start ?? DateTime.Now.AddMinutes(-5));

        var loc2 = MakeLocation(2, LocationStatus.Occupied, currentLotId: 1);
        var loc3 = MakeLocation(3, LocationStatus.Occupied, currentLotId: 2);

        // LOT-001: Loc2 → Loc3
        var lot1Route = new List<LotRoute>
        {
            MakeStep(1, 1, locationId: 2, stepOrder: 1),
            MakeStep(2, 1, locationId: 3, stepOrder: 2)
        };

        // LOT-002: Loc3 → Loc2
        var lot2Route = new List<LotRoute>
        {
            MakeStep(3, 2, locationId: 3, stepOrder: 1),
            MakeStep(4, 2, locationId: 2, stepOrder: 2)
        };

        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lot1);
        _lotRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(lot2);
        _lotRepo.Setup(r => r.UpdateAsync(It.IsAny<Lot>()))
                .Returns(Task.CompletedTask);

        _routeRepo.Setup(r => r.GetByLotIdAsync(1)).ReturnsAsync(lot1Route);
        _routeRepo.Setup(r => r.GetByLotIdAsync(2)).ReturnsAsync(lot2Route);
        _routeRepo.Setup(r => r.UpdateAsync(It.IsAny<LotRoute>()))
                  .Returns(Task.CompletedTask);

        // ← lambda so mutations are visible each call
        _locationRepo.Setup(r => r.GetByIdAsync(2))
                     .ReturnsAsync(() => loc2);
        _locationRepo.Setup(r => r.GetByIdAsync(3))
                     .ReturnsAsync(() => loc3);
        _locationRepo.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(() =>
                         new List<ProcessLocation> { loc2, loc3 });
        _locationRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcessLocation>()))
                     .Returns(Task.CompletedTask);

        _carrierRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                    .ReturnsAsync(new Carrier
                    {
                        CarrierId = 1,
                        CarrierCode = "C-01",
                        Status = "Allocated"
                    });
        _carrierRepo.Setup(r => r.UpdateAsync(It.IsAny<Carrier>()))
                    .Returns(Task.CompletedTask);

        return (lot1, lot2, loc2, loc3, lot1Route, lot2Route);
    }

    // ── DEADLOCK DETECTION ────────────────────────────────────────

    [Fact]
    public async Task MoveToNextAsync_DeadlockDetected_ReturnsSuccess()
    {
        var svc = CreateService();
        var (lot1, lot2, loc2, loc3, _, _) = SetupDeadlock();

        var result = await svc.MoveToNextAsync(1);

        Assert.True(result.Success);
        Assert.Contains("resolved", result.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MoveToNextAsync_DeadlockDetected_LogsWarning()
    {
        var svc = CreateService();
        SetupDeadlock();

        await svc.MoveToNextAsync(1);

        _logRepo.Verify(r => r.AddAsync(
            It.Is<ApplicationLog>(l =>
                l.Level == "WARN" &&
                l.Message.Contains("Deadlock"))),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task MoveToNextAsync_NotDeadlock_JustOccupied_ReturnsFalse()
    {
        var svc = CreateService();

        // LOT-002 wants Location 5, NOT Location 2 — not a deadlock
        var lot1 = MakeLot(1, currentLocationId: 2);
        var lot2 = MakeLot(2, currentLocationId: 3);
        var loc2 = MakeLocation(2, LocationStatus.Occupied, currentLotId: 1);
        var loc3 = MakeLocation(3, LocationStatus.Occupied, currentLotId: 2);

        var lot1Route = new List<LotRoute>
        {
            MakeStep(1, 1, locationId: 2, stepOrder: 1),
            MakeStep(2, 1, locationId: 3, stepOrder: 2)
        };
        var lot2Route = new List<LotRoute>
        {
            MakeStep(3, 2, locationId: 3, stepOrder: 1),
            MakeStep(4, 2, locationId: 5, stepOrder: 2) // wants loc 5, not loc 2
        };

        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lot1);
        _lotRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(lot2);
        _routeRepo.Setup(r => r.GetByLotIdAsync(1)).ReturnsAsync(lot1Route);
        _routeRepo.Setup(r => r.GetByLotIdAsync(2)).ReturnsAsync(lot2Route);
        _locationRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(() => loc2);
        _locationRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(() => loc3);
        _locationRepo.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(() =>
                         new List<ProcessLocation> { loc2, loc3 });

        var result = await svc.MoveToNextAsync(1);

        Assert.False(result.Success);
        Assert.DoesNotContain("deadlock", result.Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("occupied", result.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    // ── PRIORITY RESOLUTION ───────────────────────────────────────

    [Fact]
    public async Task AutoResolve_OlderLotHasPriority_NewerLotBecomesQueued()
    {
        var svc = CreateService();
        var (_, lot2, _, _, _, _) = SetupDeadlock(
            lot1Start: DateTime.Now.AddMinutes(-10), // older = priority
            lot2Start: DateTime.Now.AddMinutes(-5)); // newer = waits

        await svc.MoveToNextAsync(1);

        Assert.Equal(LotStatus.Queued, lot2.LotStatus);
        Assert.Null(lot2.CurrentProcessLocationId);
    }

    [Fact]
    public async Task AutoResolve_OlderLotHasPriority_MovesForward()
    {
        var svc = CreateService();
        var (lot1, _, _, _, _, _) = SetupDeadlock(
            lot1Start: DateTime.Now.AddMinutes(-10),
            lot2Start: DateTime.Now.AddMinutes(-5));

        await svc.MoveToNextAsync(1);

        // LOT-001 (older) should be Processing
        Assert.Equal(LotStatus.Processing, lot1.LotStatus);
    }

    [Fact]
    public async Task AutoResolve_WaitingLotLocationFreed_PriorityLotMovedThere()
    {
        var svc = CreateService();
        var (lot1, lot2, loc2, loc3, _, _) = SetupDeadlock();

        await svc.MoveToNextAsync(1);

        // loc3 was freed from LOT-002
        // then LOT-001 moved into it
        // so loc3 ends up Occupied by LOT-001
        Assert.Equal(LocationStatus.Occupied, loc3.Status);
        Assert.Equal(1, loc3.CurrentLotId);  // LOT-001 is there now

        // LOT-002 should be Queued with no location
        Assert.Equal(LotStatus.Queued, lot2.LotStatus);
        Assert.Null(lot2.CurrentProcessLocationId);

        // loc2 (where LOT-001 was) should now be free
        Assert.Equal(LocationStatus.Available, loc2.Status);
        Assert.Null(loc2.CurrentLotId);
    }

    [Fact]
    public async Task AutoResolve_PriorityLotCurrentLocationFreed()
    {
        var svc = CreateService();
        var (_, _, loc2, _, _, _) = SetupDeadlock();

        await svc.MoveToNextAsync(1);

        // LOT-001 moved away from Location 2 — must be freed
        Assert.Equal(LocationStatus.Available, loc2.Status);
        Assert.Null(loc2.CurrentLotId);
    }

    [Fact]
    public async Task AutoResolve_SameStartTime_CurrentLotWins()
    {
        var svc = CreateService();
        var sameTime = DateTime.Now.AddMinutes(-5);
        var (lot1, lot2, _, _, _, _) = SetupDeadlock(
            lot1Start: sameTime,
            lot2Start: sameTime);

        await svc.MoveToNextAsync(1);

        // current lot (LOT-001) triggered the call
        // with equal time currentHasPriority = true (<=)
        Assert.Equal(LotStatus.Processing, lot1.LotStatus);
        Assert.Equal(LotStatus.Queued, lot2.LotStatus);
    }

    // ── RESUME QUEUED LOT ─────────────────────────────────────────

    [Fact]
    public async Task ResumeQueuedLotAsync_LocationAvailable_ResumesLot()
    {
        var svc = CreateService();

        var lot2 = MakeLot(2, status: LotStatus.Queued,
                           currentLocationId: null);
        var loc2 = MakeLocation(2, LocationStatus.Available);

        // step 1 already completed — resumes from step 2
        var lot2Route = new List<LotRoute>
        {
            MakeStep(3, 2, locationId: 3, stepOrder: 1, isCompleted: true),
            MakeStep(4, 2, locationId: 2, stepOrder: 2, isCompleted: false)
        };

        _lotRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(lot2);
        _lotRepo.Setup(r => r.UpdateAsync(It.IsAny<Lot>()))
                .Returns(Task.CompletedTask);
        _routeRepo.Setup(r => r.GetByLotIdAsync(2)).ReturnsAsync(lot2Route);
        _locationRepo.Setup(r => r.GetByIdAsync(2))
                     .ReturnsAsync(() => loc2);
        _locationRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcessLocation>()))
                     .Returns(Task.CompletedTask);

        var result = await svc.ResumeQueuedLotAsync(2);

        Assert.True(result.Success);
        Assert.Equal(LotStatus.Processing, lot2.LotStatus);
        Assert.Equal(2, lot2.CurrentProcessLocationId);
        Assert.Equal(LocationStatus.Occupied, loc2.Status);
        Assert.Equal(2, loc2.CurrentLotId);
    }

    [Fact]
    public async Task ResumeQueuedLotAsync_LocationStillOccupied_StaysQueued()
    {
        var svc = CreateService();

        var lot2 = MakeLot(2, status: LotStatus.Queued,
                           currentLocationId: null);
        var loc2 = MakeLocation(2, LocationStatus.Occupied, currentLotId: 99);

        var lot2Route = new List<LotRoute>
        {
            MakeStep(3, 2, locationId: 3, stepOrder: 1, isCompleted: true),
            MakeStep(4, 2, locationId: 2, stepOrder: 2, isCompleted: false)
        };

        _lotRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(lot2);
        _routeRepo.Setup(r => r.GetByLotIdAsync(2)).ReturnsAsync(lot2Route);
        _locationRepo.Setup(r => r.GetByIdAsync(2))
                     .ReturnsAsync(() => loc2);

        var result = await svc.ResumeQueuedLotAsync(2);

        Assert.False(result.Success);
        Assert.Equal(LotStatus.Queued, lot2.LotStatus);
        Assert.Null(lot2.CurrentProcessLocationId);
    }

    [Fact]
    public async Task ResumeQueuedLotAsync_NoMoreSteps_CompletesLot()
    {
        var svc = CreateService();

        var lot2 = MakeLot(2, status: LotStatus.Queued,
                           currentLocationId: null,
                           carrierId: 1);

        // all steps completed
        var lot2Route = new List<LotRoute>
        {
            MakeStep(3, 2, locationId: 3, stepOrder: 1, isCompleted: true),
            MakeStep(4, 2, locationId: 2, stepOrder: 2, isCompleted: true)
        };

        _lotRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(lot2);
        _lotRepo.Setup(r => r.UpdateAsync(It.IsAny<Lot>()))
                .Returns(Task.CompletedTask);
        _routeRepo.Setup(r => r.GetByLotIdAsync(2)).ReturnsAsync(lot2Route);

        // no carrier lookup needed in ResumeQueuedLotAsync
        // carrier is only released in MoveToNextAsync last step

        var result = await svc.ResumeQueuedLotAsync(2);

        Assert.True(result.Success);
        Assert.Equal(LotStatus.Completed, lot2.LotStatus);
        Assert.NotNull(lot2.CompletedOn);
    }

    [Fact]
    public async Task ResumeQueuedLotAsync_LotNotQueued_ReturnsFalse()
    {
        var svc = CreateService();

        var lot = MakeLot(1, status: LotStatus.Processing);
        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lot);

        var result = await svc.ResumeQueuedLotAsync(1);

        Assert.False(result.Success);
        Assert.Contains("not in Queued", result.Message);
    }

    [Fact]
    public async Task ResumeQueuedLotAsync_LotNotFound_ReturnsFalse()
    {
        var svc = CreateService();
        _lotRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Lot?)null);

        var result = await svc.ResumeQueuedLotAsync(99);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    // ── EDGE CASES ────────────────────────────────────────────────

    [Fact]
    public async Task IsDeadlock_BlockingLotHasNoNextStep_NotDeadlock()
    {
        var svc = CreateService();

        var lot1 = MakeLot(1, currentLocationId: 2);
        var lot2 = MakeLot(2, currentLocationId: 3);
        var loc2 = MakeLocation(2, LocationStatus.Occupied, currentLotId: 1);
        var loc3 = MakeLocation(3, LocationStatus.Occupied, currentLotId: 2);

        var lot1Route = new List<LotRoute>
        {
            MakeStep(1, 1, locationId: 2, stepOrder: 1),
            MakeStep(2, 1, locationId: 3, stepOrder: 2)
        };

        // LOT-002 has only one step — no next step
        var lot2Route = new List<LotRoute>
        {
            MakeStep(3, 2, locationId: 3, stepOrder: 1)
        };

        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lot1);
        _lotRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(lot2);
        _routeRepo.Setup(r => r.GetByLotIdAsync(1)).ReturnsAsync(lot1Route);
        _routeRepo.Setup(r => r.GetByLotIdAsync(2)).ReturnsAsync(lot2Route);
        _locationRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(() => loc2);
        _locationRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(() => loc3);
        _locationRepo.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(() =>
                         new List<ProcessLocation> { loc2, loc3 });

        var result = await svc.MoveToNextAsync(1);

        // normal occupied wait — not deadlock
        Assert.False(result.Success);
        Assert.DoesNotContain("deadlock", result.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IsDeadlock_SingleLot_CannotDeadlock()
    {
        var svc = CreateService();

        var lot1 = MakeLot(1, currentLocationId: 2);
        var loc2 = MakeLocation(2, LocationStatus.Occupied, currentLotId: 1);
        var loc3 = MakeLocation(3, LocationStatus.Available);

        var lot1Route = new List<LotRoute>
        {
            MakeStep(1, 1, locationId: 2, stepOrder: 1),
            MakeStep(2, 1, locationId: 3, stepOrder: 2)
        };

        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lot1);
        _lotRepo.Setup(r => r.UpdateAsync(It.IsAny<Lot>()))
                .Returns(Task.CompletedTask);
        _routeRepo.Setup(r => r.GetByLotIdAsync(1)).ReturnsAsync(lot1Route);
        _routeRepo.Setup(r => r.UpdateAsync(It.IsAny<LotRoute>()))
                  .Returns(Task.CompletedTask);
        _locationRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(() => loc2);
        _locationRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(() => loc3);
        _locationRepo.Setup(r => r.GetAllAsync())
                     .ReturnsAsync(() =>
                         new List<ProcessLocation> { loc2, loc3 });
        _locationRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcessLocation>()))
                     .Returns(Task.CompletedTask);

        var result = await svc.MoveToNextAsync(1);

        Assert.True(result.Success);
        Assert.Contains("Moved", result.Message);
    }
}