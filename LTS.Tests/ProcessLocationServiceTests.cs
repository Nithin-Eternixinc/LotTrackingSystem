using LTS.Application.Services;
using LTS.Common.Constants;
using LTS.Common.Interfaces;
using LTS.Common.Models;
using Moq;
using Xunit;

namespace LTS.Tests;

public class ProcessLocationServiceTests
{
    // ── HELPERS ───────────────────────────────────────────────────

    private Mock<IProcessLocationRepository> _locationRepo = new();
    private Mock<ILotRepository> _lotRepo = new();
    private Mock<ILotRouteRepository> _routeRepo = new();
    private Mock<ICarrierRepository> _carrierRepo = new();
    private Mock<ILogRepository> _logRepo = new();

    private ProcessLocationService CreateService()
    {
        var logService = new LogService(_logRepo.Object);

        // suppress all log calls so they don't throw
        _logRepo.Setup(r => r.AddAsync(It.IsAny<ApplicationLog>()))
                .Returns(Task.CompletedTask);

        return new ProcessLocationService(
            _locationRepo.Object,
            _lotRepo.Object,
            _routeRepo.Object,
            _carrierRepo.Object,
            logService);
    }

    private Lot MakeLot(int id = 1, string status = LotStatus.Idle, int carrierId = 1)
        => new Lot
        {
            LotId = id,
            LotName = $"LOT-00{id}",
            LotStatus = status,
            CarrierId = carrierId
        };

    private ProcessLocation MakeLocation(int id = 1, string status = LocationStatus.Available)
        => new ProcessLocation
        {
            ProcessLocationId = id,
            Name = $"Location {id}",
            Status = status
        };

    private LotRoute MakeStep(int stepOrder, int locationId, bool isCompleted = false, int lotRouteId = 0)
        => new LotRoute
        {
            LotRouteId = lotRouteId == 0 ? stepOrder : lotRouteId,
            LotId = 1,
            ProcessLocationId = locationId,
            StepOrder = stepOrder,
            IsCompleted = isCompleted
        };

    // ── SAVE ROUTE ────────────────────────────────────────────────

    [Fact]
    public async Task SaveRouteAsync_EmptyList_ReturnsFalse()
    {
        var svc = CreateService();

        var result = await svc.SaveRouteAsync(1, new List<int>());

        Assert.False(result.Success);
        Assert.Contains("No locations", result.Message);
    }

    [Fact]
    public async Task SaveRouteAsync_NullList_ReturnsFalse()
    {
        var svc = CreateService();

        var result = await svc.SaveRouteAsync(1, null!);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task SaveRouteAsync_ValidLocations_DeletesOldRouteFirst()
    {
        var svc = CreateService();
        _routeRepo.Setup(r => r.DeleteByLotIdAsync(1)).Returns(Task.CompletedTask);
        _routeRepo.Setup(r => r.AddAsync(It.IsAny<LotRoute>())).Returns(Task.CompletedTask);

        await svc.SaveRouteAsync(1, new List<int> { 2, 5, 7 });

        // verify old route was deleted before adding new one
        _routeRepo.Verify(r => r.DeleteByLotIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task SaveRouteAsync_ValidLocations_SavesCorrectStepOrder()
    {
        var svc = CreateService();
        var savedSteps = new List<LotRoute>();

        _routeRepo.Setup(r => r.DeleteByLotIdAsync(It.IsAny<int>()))
                  .Returns(Task.CompletedTask);

        _routeRepo.Setup(r => r.AddAsync(It.IsAny<LotRoute>()))
                  .Callback<LotRoute>(step => savedSteps.Add(step))
                  .Returns(Task.CompletedTask);

        await svc.SaveRouteAsync(1, new List<int> { 2, 5, 7 });

        Assert.Equal(3, savedSteps.Count);
        Assert.Equal(1, savedSteps[0].StepOrder);
        Assert.Equal(2, savedSteps[1].StepOrder);
        Assert.Equal(3, savedSteps[2].StepOrder);
        Assert.Equal(2, savedSteps[0].ProcessLocationId);
        Assert.Equal(5, savedSteps[1].ProcessLocationId);
        Assert.Equal(7, savedSteps[2].ProcessLocationId);
    }

    [Fact]
    public async Task SaveRouteAsync_ValidLocations_ReturnsSuccess()
    {
        var svc = CreateService();
        _routeRepo.Setup(r => r.DeleteByLotIdAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _routeRepo.Setup(r => r.AddAsync(It.IsAny<LotRoute>())).Returns(Task.CompletedTask);

        var result = await svc.SaveRouteAsync(1, new List<int> { 2, 5 });

        Assert.True(result.Success);
    }

    // ── START LOT ─────────────────────────────────────────────────

    [Fact]
    public async Task StartLotAsync_LotNotFound_ReturnsFalse()
    {
        var svc = CreateService();
        _lotRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Lot?)null);

        var result = await svc.StartLotAsync(99);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task StartLotAsync_LotNotIdle_ReturnsFalse()
    {
        var svc = CreateService();
        _lotRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(MakeLot(status: LotStatus.Processing));

        var result = await svc.StartLotAsync(1);

        Assert.False(result.Success);
        Assert.Contains("Idle", result.Message);
    }

    [Fact]
    public async Task StartLotAsync_NoRouteDefined_ReturnsFalse()
    {
        var svc = CreateService();
        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeLot());
        _routeRepo.Setup(r => r.GetByLotIdAsync(1))
                  .ReturnsAsync(new List<LotRoute>()); // empty route

        var result = await svc.StartLotAsync(1);

        Assert.False(result.Success);
        Assert.Contains("No route", result.Message);
    }

    [Fact]
    public async Task StartLotAsync_FirstLocationOccupied_ReturnsFalse()
    {
        var svc = CreateService();
        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeLot());
        _routeRepo.Setup(r => r.GetByLotIdAsync(1))
                  .ReturnsAsync(new List<LotRoute> { MakeStep(1, locationId: 3) });
        _locationRepo.Setup(r => r.GetByIdAsync(3))
                     .ReturnsAsync(MakeLocation(3, LocationStatus.Occupied));

        var result = await svc.StartLotAsync(1);

        Assert.False(result.Success);
        Assert.Contains("occupied", result.Message);
    }

    [Fact]
    public async Task StartLotAsync_FirstLocationNotFound_ReturnsFalse()
    {
        var svc = CreateService();
        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeLot());
        _routeRepo.Setup(r => r.GetByLotIdAsync(1))
                  .ReturnsAsync(new List<LotRoute> { MakeStep(1, locationId: 99) });
        _locationRepo.Setup(r => r.GetByIdAsync(99))
                     .ReturnsAsync((ProcessLocation?)null);

        var result = await svc.StartLotAsync(1);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task StartLotAsync_ValidLot_AssignsLocationAndUpdatesLotStatus()
    {
        var svc = CreateService();
        var lot = MakeLot();
        var location = MakeLocation(3);

        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lot);
        _routeRepo.Setup(r => r.GetByLotIdAsync(1))
                  .ReturnsAsync(new List<LotRoute> { MakeStep(1, locationId: 3) });
        _locationRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(location);
        _locationRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcessLocation>())).Returns(Task.CompletedTask);
        _lotRepo.Setup(r => r.UpdateAsync(It.IsAny<Lot>())).Returns(Task.CompletedTask);

        var result = await svc.StartLotAsync(1);

        Assert.True(result.Success);
        Assert.Equal(LocationStatus.Occupied, location.Status);
        Assert.Equal(1, location.CurrentLotId);
        Assert.Equal(LotStatus.Processing, lot.LotStatus);
        Assert.Equal(3, lot.CurrentProcessLocationId);
    }

    [Fact]
    public async Task StartLotAsync_ValidLot_SetsStartedOn()
    {
        var svc = CreateService();
        var lot = MakeLot();

        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lot);
        _routeRepo.Setup(r => r.GetByLotIdAsync(1))
                  .ReturnsAsync(new List<LotRoute> { MakeStep(1, locationId: 3) });
        _locationRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(MakeLocation(3));
        _locationRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcessLocation>())).Returns(Task.CompletedTask);
        _lotRepo.Setup(r => r.UpdateAsync(It.IsAny<Lot>())).Returns(Task.CompletedTask);

        await svc.StartLotAsync(1);

        Assert.NotNull(lot.StartedOn);
        Assert.True(lot.StartedOn <= DateTime.Now);
    }

    // ── MOVE TO NEXT ──────────────────────────────────────────────

    [Fact]
    public async Task MoveToNextAsync_LotNotFound_ReturnsFalse()
    {
        var svc = CreateService();
        _lotRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Lot?)null);

        var result = await svc.MoveToNextAsync(99);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task MoveToNextAsync_LotNotProcessing_ReturnsFalse()
    {
        var svc = CreateService();
        _lotRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(MakeLot(status: LotStatus.Idle));

        var result = await svc.MoveToNextAsync(1);

        Assert.False(result.Success);
        Assert.Contains("not processing", result.Message);
    }

    [Fact]
    public async Task MoveToNextAsync_NoActiveStep_ReturnsFalse()
    {
        var svc = CreateService();
        _lotRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(MakeLot(status: LotStatus.Processing));

        // all steps already completed
        _routeRepo.Setup(r => r.GetByLotIdAsync(1))
                  .ReturnsAsync(new List<LotRoute>
                  {
                      MakeStep(1, locationId: 2, isCompleted: true),
                      MakeStep(2, locationId: 5, isCompleted: true)
                  });

        var result = await svc.MoveToNextAsync(1);

        Assert.False(result.Success);
        Assert.Contains("No active", result.Message);
    }

    [Fact]
    public async Task MoveToNextAsync_NextLocationOccupied_ReturnsFalse()
    {
        var svc = CreateService();
        _lotRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(MakeLot(status: LotStatus.Processing));

        _routeRepo.Setup(r => r.GetByLotIdAsync(1))
                  .ReturnsAsync(new List<LotRoute>
                  {
                      MakeStep(1, locationId: 2, lotRouteId: 1),
                      MakeStep(2, locationId: 5, lotRouteId: 2)
                  });

        _locationRepo.Setup(r => r.GetByIdAsync(5))
                     .ReturnsAsync(MakeLocation(5, LocationStatus.Occupied));

        var result = await svc.MoveToNextAsync(1);

        Assert.False(result.Success);
        Assert.Contains("occupied", result.Message);
    }

    [Fact]
    public async Task MoveToNextAsync_NextLocationOccupied_DoesNotChangeAnything()
    {
        var svc = CreateService();
        _lotRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(MakeLot(status: LotStatus.Processing));

        _routeRepo.Setup(r => r.GetByLotIdAsync(1))
                  .ReturnsAsync(new List<LotRoute>
                  {
                      MakeStep(1, locationId: 2, lotRouteId: 1),
                      MakeStep(2, locationId: 5, lotRouteId: 2)
                  });

        _locationRepo.Setup(r => r.GetByIdAsync(5))
                     .ReturnsAsync(MakeLocation(5, LocationStatus.Occupied));

        await svc.MoveToNextAsync(1);

        // nothing should have been updated
        _routeRepo.Verify(r => r.UpdateAsync(It.IsAny<LotRoute>()), Times.Never);
        _locationRepo.Verify(r => r.UpdateAsync(It.IsAny<ProcessLocation>()), Times.Never);
        _lotRepo.Verify(r => r.UpdateAsync(It.IsAny<Lot>()), Times.Never);
    }

    [Fact]
    public async Task MoveToNextAsync_ValidMove_FreesCurrentLocation()
    {
        var svc = CreateService();
        var lot = MakeLot(status: LotStatus.Processing);
        var currentLocation = MakeLocation(2, LocationStatus.Occupied);
        currentLocation.CurrentLotId = 1;
        var nextLocation = MakeLocation(5);

        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lot);
        _routeRepo.Setup(r => r.GetByLotIdAsync(1))
                  .ReturnsAsync(new List<LotRoute>
                  {
                      MakeStep(1, locationId: 2, lotRouteId: 1),
                      MakeStep(2, locationId: 5, lotRouteId: 2)
                  });
        _locationRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(nextLocation);
        _locationRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(currentLocation);
        _locationRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcessLocation>())).Returns(Task.CompletedTask);
        _routeRepo.Setup(r => r.UpdateAsync(It.IsAny<LotRoute>())).Returns(Task.CompletedTask);
        _lotRepo.Setup(r => r.UpdateAsync(It.IsAny<Lot>())).Returns(Task.CompletedTask);

        await svc.MoveToNextAsync(1);

        Assert.Equal(LocationStatus.Available, currentLocation.Status);
        Assert.Null(currentLocation.CurrentLotId);
    }

    [Fact]
    public async Task MoveToNextAsync_ValidMove_OccupiesNextLocation()
    {
        var svc = CreateService();
        var lot = MakeLot(status: LotStatus.Processing);
        var nextLocation = MakeLocation(5);

        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lot);
        _routeRepo.Setup(r => r.GetByLotIdAsync(1))
                  .ReturnsAsync(new List<LotRoute>
                  {
                      MakeStep(1, locationId: 2, lotRouteId: 1),
                      MakeStep(2, locationId: 5, lotRouteId: 2)
                  });
        _locationRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(nextLocation);
        _locationRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(MakeLocation(2, LocationStatus.Occupied));
        _locationRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcessLocation>())).Returns(Task.CompletedTask);
        _routeRepo.Setup(r => r.UpdateAsync(It.IsAny<LotRoute>())).Returns(Task.CompletedTask);
        _lotRepo.Setup(r => r.UpdateAsync(It.IsAny<Lot>())).Returns(Task.CompletedTask);

        await svc.MoveToNextAsync(1);

        Assert.Equal(LocationStatus.Occupied, nextLocation.Status);
        Assert.Equal(1, nextLocation.CurrentLotId);
    }

    [Fact]
    public async Task MoveToNextAsync_ValidMove_UpdatesLotCurrentLocation()
    {
        var svc = CreateService();
        var lot = MakeLot(status: LotStatus.Processing);

        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lot);
        _routeRepo.Setup(r => r.GetByLotIdAsync(1))
                  .ReturnsAsync(new List<LotRoute>
                  {
                      MakeStep(1, locationId: 2, lotRouteId: 1),
                      MakeStep(2, locationId: 5, lotRouteId: 2)
                  });
        _locationRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(MakeLocation(5));
        _locationRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(MakeLocation(2, LocationStatus.Occupied));
        _locationRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcessLocation>())).Returns(Task.CompletedTask);
        _routeRepo.Setup(r => r.UpdateAsync(It.IsAny<LotRoute>())).Returns(Task.CompletedTask);
        _lotRepo.Setup(r => r.UpdateAsync(It.IsAny<Lot>())).Returns(Task.CompletedTask);

        await svc.MoveToNextAsync(1);

        Assert.Equal(5, lot.CurrentProcessLocationId);
    }

    [Fact]
    public async Task MoveToNextAsync_LastStep_CompletesLot()
    {
        var svc = CreateService();
        var lot = MakeLot(status: LotStatus.Processing, carrierId: 1);

        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lot);

        // only one step — no next step after it
        _routeRepo.Setup(r => r.GetByLotIdAsync(1))
                  .ReturnsAsync(new List<LotRoute>
                  {
                      MakeStep(1, locationId: 2, lotRouteId: 1)
                  });

        _locationRepo.Setup(r => r.GetByIdAsync(2))
                     .ReturnsAsync(MakeLocation(2, LocationStatus.Occupied));
        _locationRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcessLocation>())).Returns(Task.CompletedTask);
        _routeRepo.Setup(r => r.UpdateAsync(It.IsAny<LotRoute>())).Returns(Task.CompletedTask);
        _lotRepo.Setup(r => r.UpdateAsync(It.IsAny<Lot>())).Returns(Task.CompletedTask);
        _carrierRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Carrier { CarrierId = 1, CarrierCode = "C-01", Status = "Allocated" });
        _carrierRepo.Setup(r => r.UpdateAsync(It.IsAny<Carrier>())).Returns(Task.CompletedTask);

        var result = await svc.MoveToNextAsync(1);

        Assert.True(result.Success);
        Assert.Equal(LotStatus.Completed, lot.LotStatus);
        Assert.Null(lot.CurrentProcessLocationId);
        Assert.Contains("completed", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MoveToNextAsync_LastStep_SetsCompletedOn()
    {
        var svc = CreateService();
        var lot = MakeLot(status: LotStatus.Processing, carrierId: 1);

        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lot);
        _routeRepo.Setup(r => r.GetByLotIdAsync(1))
                  .ReturnsAsync(new List<LotRoute> { MakeStep(1, locationId: 2, lotRouteId: 1) });
        _locationRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(MakeLocation(2, LocationStatus.Occupied));
        _locationRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcessLocation>())).Returns(Task.CompletedTask);
        _routeRepo.Setup(r => r.UpdateAsync(It.IsAny<LotRoute>())).Returns(Task.CompletedTask);
        _lotRepo.Setup(r => r.UpdateAsync(It.IsAny<Lot>())).Returns(Task.CompletedTask);
        _carrierRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Carrier { CarrierId = 1, Status = "Allocated" });
        _carrierRepo.Setup(r => r.UpdateAsync(It.IsAny<Carrier>())).Returns(Task.CompletedTask);

        await svc.MoveToNextAsync(1);

        Assert.NotNull(lot.CompletedOn);
        Assert.True(lot.CompletedOn <= DateTime.Now);
    }

    [Fact]
    public async Task MoveToNextAsync_LastStep_ReleasesCarrier()
    {
        var svc = CreateService();
        var lot = MakeLot(status: LotStatus.Processing, carrierId: 1);
        var carrier = new Carrier { CarrierId = 1, CarrierCode = "C-01", Status = "Allocated" };

        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lot);
        _routeRepo.Setup(r => r.GetByLotIdAsync(1))
                  .ReturnsAsync(new List<LotRoute> { MakeStep(1, locationId: 2, lotRouteId: 1) });
        _locationRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(MakeLocation(2, LocationStatus.Occupied));
        _locationRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcessLocation>())).Returns(Task.CompletedTask);
        _routeRepo.Setup(r => r.UpdateAsync(It.IsAny<LotRoute>())).Returns(Task.CompletedTask);
        _lotRepo.Setup(r => r.UpdateAsync(It.IsAny<Lot>())).Returns(Task.CompletedTask);
        _carrierRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(carrier);
        _carrierRepo.Setup(r => r.UpdateAsync(It.IsAny<Carrier>())).Returns(Task.CompletedTask);

        await svc.MoveToNextAsync(1);

        Assert.Equal("Unallocated", carrier.Status);
        _carrierRepo.Verify(r => r.UpdateAsync(carrier), Times.Once);
    }

    [Fact]
    public async Task MoveToNextAsync_LastStep_FreesCurrentLocation()
    {
        var svc = CreateService();
        var lot = MakeLot(status: LotStatus.Processing, carrierId: 1);
        var currentLocation = MakeLocation(2, LocationStatus.Occupied);
        currentLocation.CurrentLotId = 1;

        _lotRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lot);
        _routeRepo.Setup(r => r.GetByLotIdAsync(1))
                  .ReturnsAsync(new List<LotRoute> { MakeStep(1, locationId: 2, lotRouteId: 1) });
        _locationRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(currentLocation);
        _locationRepo.Setup(r => r.UpdateAsync(It.IsAny<ProcessLocation>())).Returns(Task.CompletedTask);
        _routeRepo.Setup(r => r.UpdateAsync(It.IsAny<LotRoute>())).Returns(Task.CompletedTask);
        _lotRepo.Setup(r => r.UpdateAsync(It.IsAny<Lot>())).Returns(Task.CompletedTask);
        _carrierRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Carrier { CarrierId = 1, Status = "Allocated" });
        _carrierRepo.Setup(r => r.UpdateAsync(It.IsAny<Carrier>())).Returns(Task.CompletedTask);

        await svc.MoveToNextAsync(1);

        Assert.Equal(LocationStatus.Available, currentLocation.Status);
        Assert.Null(currentLocation.CurrentLotId);
    }
}