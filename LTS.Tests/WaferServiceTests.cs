using LTS.Application.Services;
using LTS.Common.Interfaces;
using LTS.Common.Models;
using Moq;

namespace LTS.Tests;

public class WaferServiceTests
{
    private readonly Mock<IWaferRepository> _waferRepoMock;
    private readonly WaferService _waferService;

    public WaferServiceTests()
    {
        _waferRepoMock = new Mock<IWaferRepository>();

        var logRepoMock = new Mock<ILogRepository>();
        logRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ApplicationLog>()))
            .Returns(Task.CompletedTask);

        var logService = new LogService(logRepoMock.Object);

        _waferService = new WaferService(
            _waferRepoMock.Object,
            logService);
    }

    [Fact]
    public async Task AddWafer_ShouldFail_WhenSerialNumberIsDuplicate()
    {
        // ARRANGE
        _waferRepoMock
            .Setup(r => r.ExistsBySerialAsync("W-001"))
            .ReturnsAsync(true);  // ← duplicate serial

        var wafer = new WaferMaster
        {
            WaferSerialNo = "W-001",
            SupplierId = 1
        };

        // ACT
        var result = await _waferService.AddWaferAsync(wafer);

        // ASSERT
        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message);
    }

    [Fact]
    public async Task DeleteWafer_ShouldFail_WhenWaferIsAllocated()
    {
        // ARRANGE — wafer is currently in an active lot
        _waferRepoMock
            .Setup(r => r.IsAllocatedAsync(5))
            .ReturnsAsync(true);  // ← allocated, cannot delete

        // ACT
        var result = await _waferService.DeleteWaferAsync(5);

        // ASSERT
        Assert.False(result.Success);
        Assert.Contains("Cannot delete", result.Message);
    }

    [Fact]
    public async Task AddWafer_ShouldFail_WhenSerialNumberIsEmpty()
    {
        var wafer = new WaferMaster
        {
            WaferSerialNo = "",   // ← empty
            SupplierId = 1
        };

        var result = await _waferService.AddWaferAsync(wafer);

        Assert.False(result.Success);
        Assert.Contains("required", result.Message);
    }
}