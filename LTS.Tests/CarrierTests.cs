using LTS.Application.Services;
using LTS.Common.Interfaces;
using LTS.Common.Models;
using Moq;
using Xunit;

namespace LTS.Tests;

public class CarrierServiceTests
{
    // ── HELPERS ───────────────────────────────────────────────────

    private Mock<ICarrierRepository> _carrierRepo = new();
    private Mock<ILogRepository> _logRepo = new();

    private CarrierService CreateService()
    {
        _logRepo.Setup(r => r.AddAsync(It.IsAny<ApplicationLog>()))
                .Returns(Task.CompletedTask);

        var logService = new LogService(_logRepo.Object);
        return new CarrierService(_carrierRepo.Object, logService);
    }

    private Carrier MakeCarrier(
        int id = 1,
        string code = "C-01",
        string status = "Unallocated",
        int capacity = 25)
        => new Carrier
        {
            CarrierId = id,
            CarrierCode = code,
            Status = status,
            Capacity = capacity,
            CurrentLocation = "None",
            CreatedOn = DateTime.Now
        };

    // ── ADD CARRIER ───────────────────────────────────────────────

    [Fact]
    public async Task AddCarrierAsync_EmptyCode_ReturnsFalse()
    {
        var svc = CreateService();

        var result = await svc.AddCarrierAsync("", 25);

        Assert.False(result.Success);
        Assert.Contains("required", result.Message);
    }

    [Fact]
    public async Task AddCarrierAsync_WhitespaceCode_ReturnsFalse()
    {
        var svc = CreateService();

        var result = await svc.AddCarrierAsync("   ", 25);

        Assert.False(result.Success);
        Assert.Contains("required", result.Message);
    }

    [Fact]
    public async Task AddCarrierAsync_ZeroCapacity_ReturnsFalse()
    {
        var svc = CreateService();

        var result = await svc.AddCarrierAsync("C-01", 0);

        Assert.False(result.Success);
        Assert.Contains("Capacity", result.Message);
    }

    [Fact]
    public async Task AddCarrierAsync_NegativeCapacity_ReturnsFalse()
    {
        var svc = CreateService();

        var result = await svc.AddCarrierAsync("C-01", -5);

        Assert.False(result.Success);
        Assert.Contains("Capacity", result.Message);
    }

    [Fact]
    public async Task AddCarrierAsync_CapacityOver25_ReturnsFalse()
    {
        var svc = CreateService();

        var result = await svc.AddCarrierAsync("C-01", 26);

        Assert.False(result.Success);
        Assert.Contains("Capacity", result.Message);
    }

    [Fact]
    public async Task AddCarrierAsync_CapacityExactly25_ReturnsSuccess()
    {
        var svc = CreateService();

        _carrierRepo.Setup(r => r.ExistsByCodeAsync("C-01"))
                    .ReturnsAsync(false);
        _carrierRepo.Setup(r => r.AddAsync(It.IsAny<Carrier>()))
                    .Returns(Task.CompletedTask);

        var result = await svc.AddCarrierAsync("C-01", 25);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task AddCarrierAsync_DuplicateCode_ReturnsFalse()
    {
        var svc = CreateService();

        _carrierRepo.Setup(r => r.ExistsByCodeAsync("C-01"))
                    .ReturnsAsync(true); // already exists

        var result = await svc.AddCarrierAsync("C-01", 25);

        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message);
    }

    [Fact]
    public async Task AddCarrierAsync_ValidInput_SavesCarrier()
    {
        var svc = CreateService();
        Carrier? saved = null;

        _carrierRepo.Setup(r => r.ExistsByCodeAsync("C-01"))
                    .ReturnsAsync(false);
        _carrierRepo.Setup(r => r.AddAsync(It.IsAny<Carrier>()))
                    .Callback<Carrier>(c => saved = c)
                    .Returns(Task.CompletedTask);

        var result = await svc.AddCarrierAsync("C-01", 20);

        Assert.True(result.Success);
        Assert.NotNull(saved);
        Assert.Equal("C-01", saved!.CarrierCode);
        Assert.Equal(20, saved.Capacity);
        Assert.Equal("Unallocated", saved.Status);
    }

    [Fact]
    public async Task AddCarrierAsync_ValidInput_TrimsWhitespace()
    {
        var svc = CreateService();
        Carrier? saved = null;

        _carrierRepo.Setup(r => r.ExistsByCodeAsync(It.IsAny<string>()))
                    .ReturnsAsync(false);
        _carrierRepo.Setup(r => r.AddAsync(It.IsAny<Carrier>()))
                    .Callback<Carrier>(c => saved = c)
                    .Returns(Task.CompletedTask);

        await svc.AddCarrierAsync("  C-01  ", 25);

        Assert.Equal("C-01", saved!.CarrierCode); // whitespace trimmed
    }

    [Fact]
    public async Task AddCarrierAsync_ValidInput_ReturnsSuccess()
    {
        var svc = CreateService();

        _carrierRepo.Setup(r => r.ExistsByCodeAsync("C-02"))
                    .ReturnsAsync(false);
        _carrierRepo.Setup(r => r.AddAsync(It.IsAny<Carrier>()))
                    .Returns(Task.CompletedTask);

        var result = await svc.AddCarrierAsync("C-02", 10);

        Assert.True(result.Success);
        Assert.Contains("successfully", result.Message);
    }

    [Fact]
    public async Task AddCarrierAsync_RepositoryThrows_ReturnsFalse()
    {
        var svc = CreateService();

        _carrierRepo.Setup(r => r.ExistsByCodeAsync(It.IsAny<string>()))
                    .ReturnsAsync(false);
        _carrierRepo.Setup(r => r.AddAsync(It.IsAny<Carrier>()))
                    .ThrowsAsync(new Exception("DB connection failed"));

        var result = await svc.AddCarrierAsync("C-01", 25);

        Assert.False(result.Success);
        Assert.Contains("Unexpected", result.Message);
    }

    // ── DELETE CARRIER ────────────────────────────────────────────

    [Fact]
    public async Task DeleteCarrierAsync_CarrierNotFound_ReturnsFalse()
    {
        var svc = CreateService();

        _carrierRepo.Setup(r => r.GetAllAsync())
                    .ReturnsAsync(new List<Carrier>()); // empty list

        var result = await svc.DeleteCarrierAsync(99);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task DeleteCarrierAsync_AllocatedCarrier_ReturnsFalse()
    {
        var svc = CreateService();

        _carrierRepo.Setup(r => r.GetAllAsync())
                    .ReturnsAsync(new List<Carrier>
                    {
                        MakeCarrier(id: 1, status: "Allocated")
                    });

        var result = await svc.DeleteCarrierAsync(1);

        Assert.False(result.Success);
        Assert.Contains("allocated", result.Message);
    }

    [Fact]
    public async Task DeleteCarrierAsync_AllocatedCarrier_DoesNotCallDelete()
    {
        var svc = CreateService();

        _carrierRepo.Setup(r => r.GetAllAsync())
                    .ReturnsAsync(new List<Carrier>
                    {
                        MakeCarrier(id: 1, status: "Allocated")
                    });

        await svc.DeleteCarrierAsync(1);

        // delete should never be called
        _carrierRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCarrierAsync_UnallocatedCarrier_ReturnsSuccess()
    {
        var svc = CreateService();

        _carrierRepo.Setup(r => r.GetAllAsync())
                    .ReturnsAsync(new List<Carrier>
                    {
                        MakeCarrier(id: 1, status: "Unallocated")
                    });
        _carrierRepo.Setup(r => r.DeleteAsync(1))
                    .Returns(Task.CompletedTask);

        var result = await svc.DeleteCarrierAsync(1);

        Assert.True(result.Success);
        Assert.Contains("successfully", result.Message);
    }

    [Fact]
    public async Task DeleteCarrierAsync_UnallocatedCarrier_CallsDeleteOnce()
    {
        var svc = CreateService();

        _carrierRepo.Setup(r => r.GetAllAsync())
                    .ReturnsAsync(new List<Carrier>
                    {
                        MakeCarrier(id: 1, status: "Unallocated")
                    });
        _carrierRepo.Setup(r => r.DeleteAsync(1))
                    .Returns(Task.CompletedTask);

        await svc.DeleteCarrierAsync(1);

        _carrierRepo.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteCarrierAsync_RepositoryThrows_ReturnsFalse()
    {
        var svc = CreateService();

        _carrierRepo.Setup(r => r.GetAllAsync())
                    .ReturnsAsync(new List<Carrier>
                    {
                        MakeCarrier(id: 1, status: "Unallocated")
                    });
        _carrierRepo.Setup(r => r.DeleteAsync(1))
                    .ThrowsAsync(new Exception("DB error"));

        var result = await svc.DeleteCarrierAsync(1);

        Assert.False(result.Success);
        Assert.Contains("Unexpected", result.Message);
    }

    // ── GET CARRIERS ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllCarriersAsync_ReturnsAllCarriers()
    {
        var svc = CreateService();

        _carrierRepo.Setup(r => r.GetAllAsync())
                    .ReturnsAsync(new List<Carrier>
                    {
                        MakeCarrier(1, "C-01"),
                        MakeCarrier(2, "C-02"),
                        MakeCarrier(3, "C-03")
                    });

        var result = await svc.GetAllCarriersAsync();

        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetAvailableCarriersAsync_ReturnsOnlyUnallocated()
    {
        var svc = CreateService();

        _carrierRepo.Setup(r => r.GetAvailableAsync())
                    .ReturnsAsync(new List<Carrier>
                    {
                        MakeCarrier(1, "C-01", "Unallocated"),
                        MakeCarrier(2, "C-02", "Unallocated")
                    });

        var result = await svc.GetAvailableCarriersAsync();

        Assert.Equal(2, result.Count());
        Assert.All(result, c => Assert.Equal("Unallocated", c.Status));
    }
}