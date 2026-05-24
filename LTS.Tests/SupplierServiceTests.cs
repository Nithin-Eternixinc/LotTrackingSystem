using LTS.Application.Services;
using LTS.Common.Interfaces;
using LTS.Common.Models;
using Moq;

namespace LTS.Tests;

public class SupplierServiceTests
{
    // shared setup — fake repos used in every test
    private readonly Mock<ISupplierRepository> _supplierRepoMock;
    private readonly Mock<ILogRepository> _logRepoMock;
    private readonly SupplierService _supplierService;

    public SupplierServiceTests()
    {
        _supplierRepoMock = new Mock<ISupplierRepository>();
        _logRepoMock = new Mock<ILogRepository>();

        // fake log repo — just accepts logs, does nothing
        _logRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ApplicationLog>()))
            .Returns(Task.CompletedTask);

        var logService = new LogService(_logRepoMock.Object);

        _supplierService = new SupplierService(
            _supplierRepoMock.Object,
            logService);
    }

    [Fact]
    public async Task AddSupplier_ShouldSucceed_WhenNameIsUnique()
    {
        // ARRANGE — name does not exist yet
        _supplierRepoMock
            .Setup(r => r.ExistsByNameAsync("SupplierA"))
            .ReturnsAsync(false);

        _supplierRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Supplier>()))
            .Returns(Task.CompletedTask);

        var supplier = new Supplier { SupplierName = "SupplierA" };

        // ACT
        var result = await _supplierService.AddSupplierAsync(supplier);

        // ASSERT
        Assert.True(result.Success);
        Assert.Equal("Supplier added successfully.", result.Message);
    }

    [Fact]
    public async Task AddSupplier_ShouldFail_WhenNameIsDuplicate()
    {
        // ARRANGE — name already exists
        _supplierRepoMock
            .Setup(r => r.ExistsByNameAsync("SupplierA"))
            .ReturnsAsync(true);  // ← duplicate!

        var supplier = new Supplier { SupplierName = "SupplierA" };

        // ACT
        var result = await _supplierService.AddSupplierAsync(supplier);

        // ASSERT
        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message);
    }

    [Fact]
    public async Task DeleteSupplier_ShouldFail_WhenSupplierHasWafers()
    {
        // ARRANGE — supplier has wafers linked
        _supplierRepoMock
            .Setup(r => r.HasWafersAsync(1))
            .ReturnsAsync(true);  // ← has wafers, cannot delete

        // ACT
        var result = await _supplierService.DeleteSupplierAsync(1);

        // ASSERT
        Assert.False(result.Success);
        Assert.Contains("Cannot delete", result.Message);
    }
}