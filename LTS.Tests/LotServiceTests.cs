using LTS.Application.Repositories;
using LTS.Application.Services;
using LTS.Common.Interfaces;
using LTS.Common.Models;
using Moq;

namespace LTS.Tests;

public class LotServiceTests
{
    private readonly Mock<ILotRepository> _lotRepoMock;
    private readonly Mock<IWaferRepository> _waferRepoMock;
    private readonly Mock<ILotRouteRepository> _routeRepoMock;
    private readonly Mock<ICarrierRepository> _carrierRepoMock;  // ← add this
    private readonly LotService _lotService;

    public LotServiceTests()
    {
        _lotRepoMock = new Mock<ILotRepository>();
        _waferRepoMock = new Mock<IWaferRepository>();
        _routeRepoMock = new Mock<ILotRouteRepository>();
        _carrierRepoMock = new Mock<ICarrierRepository>();  // ← add this

        var logRepoMock = new Mock<ILogRepository>();
        logRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ApplicationLog>()))
            .Returns(Task.CompletedTask);

        var logService = new LogService(logRepoMock.Object);

        // match your actual LotService constructor
        _lotService = new LotService(
            _lotRepoMock.Object,
            _waferRepoMock.Object,
             _routeRepoMock.Object,
            _carrierRepoMock.Object,  // ← add this
            logService
        );
    }


    [Fact]
    public async Task CreateLot_ShouldFail_WhenLotNameIsEmpty()
    {
        var result = await _lotService.CreateLotAsync(
            "",           // lotName — empty
            1,            // carrierId
            new List<int> { 1, 2 }  // selectedWaferIds
        );

        Assert.False(result.Success);
        Assert.Contains("required", result.Message);
    }

    [Fact]
    public async Task CreateLot_ShouldFail_WhenNoWafersSelected()
    {
        var result = await _lotService.CreateLotAsync(
            "LOT-001",
            1,
            new List<int>()  // empty list — no wafers selected
        );

        Assert.False(result.Success);
        Assert.Contains("select at least one wafer", result.Message);
    }

    [Fact]
    public async Task CreateLot_ShouldFail_WhenCarrierNotSelected()
    {
        var result = await _lotService.CreateLotAsync(
            "LOT-001",
            0,            // carrierId = 0 means not selected
            new List<int> { 1, 2 }
        );

        Assert.False(result.Success);
        Assert.Contains("carrier", result.Message);
    }

    [Fact]
    public async Task CreateLot_ShouldFail_WhenCarrierAlreadyAllocated()
    {
        // ARRANGE — carrier exists but already allocated
        _carrierRepoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Carrier>
            {
            new Carrier
            {
                CarrierId = 1,
                CarrierCode = "C-001",
                Status = "Allocated",  // ← already in use
                Capacity = 10
            }
            });

        var result = await _lotService.CreateLotAsync(
            "LOT-001",
            1,
            new List<int> { 1, 2 }
        );

        Assert.False(result.Success);
        Assert.Contains("already in use", result.Message);
    }

    [Fact]
    public async Task CreateLot_ShouldFail_WhenWaferCountExceedsCarrierCapacity()
    {
        // ARRANGE — carrier capacity is 2, but 5 wafers selected
        _carrierRepoMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Carrier>
            {
            new Carrier
            {
                CarrierId = 1,
                CarrierCode = "C-001",
                Status = "Unallocated",
                Capacity = 2  // ← only fits 2
            }
            });

        var result = await _lotService.CreateLotAsync(
            "LOT-001",
            1,
            new List<int> { 1, 2, 3, 4, 5 }  // ← 5 wafers
        );

        Assert.False(result.Success);
        Assert.Contains("exceed carrier capacity", result.Message);
    }

    [Fact]
    public async Task DeleteLot_ShouldFail_WhenLotIsProcessing()
    {
        _lotRepoMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Lot
            {
                LotId = 1,
                LotName = "LOT-001",
                LotStatus = "Processing"  // ← cannot delete
            });

        var result = await _lotService.DeleteLotAsync(1);

        Assert.False(result.Success);
        Assert.Contains("processing", result.Message);
    }

    [Fact]
    public async Task DeleteLot_ShouldFail_WhenLotIsNotIdle()
    {
        _lotRepoMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Lot
            {
                LotId = 1,
                LotName = "LOT-001",
                LotStatus = "Completed"  // ← not Idle
            });

        var result = await _lotService.DeleteLotAsync(1);

        Assert.False(result.Success);
        Assert.Contains("Idle", result.Message);
    }



}

    