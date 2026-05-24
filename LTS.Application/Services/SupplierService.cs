using System;
using System.Collections.Generic;
using System.Text;
using LTS.Common.Interfaces;
using LTS.Common.Models;

namespace LTS.Application.Services;

public class SupplierService
{
    private readonly ISupplierRepository _supplierRepo;
    private readonly LogService _log;

    public SupplierService(ISupplierRepository supplierRepo, LogService log)
    {
        _supplierRepo = supplierRepo;
        _log = log;
    }

    public async Task<IEnumerable<Supplier>> GetAllAsync()
    {
        return await _supplierRepo.GetAllAsync();
    }

    public async Task<(bool Success, string Message)> AddSupplierAsync(Supplier supplier)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(supplier.SupplierName))
                return (false, "Supplier name is required.");

            if (await _supplierRepo.ExistsByNameAsync(supplier.SupplierName))
            {
                await _log.LogWarnAsync(
                    $"Duplicate supplier name: {supplier.SupplierName}");

                return (false,
                    $"Supplier '{supplier.SupplierName}' already exists.");
            }

            supplier.CreatedOn = DateTime.Now;
            supplier.IsActive = true;

            await _supplierRepo.AddAsync(supplier);

            await _log.LogInfoAsync(
                $"Supplier '{supplier.SupplierName}' added successfully");

            return (true, "Supplier added successfully.");
        }
        catch (Exception ex)
        {
            await _log.LogErrorAsync(
                $"Error adding supplier: {ex.Message}");

            return (false, "Unexpected error occurred.");
        }
    }


    public async Task<(bool Success, string Message)> DeleteSupplierAsync(int supplierId)
    {
        try
        {
            if (await _supplierRepo.HasWafersAsync(supplierId))
            {
                await _log.LogWarnAsync(
                    $"Delete blocked — supplier ID {supplierId} has wafers linked");

                return (false,
                    "Cannot delete supplier — it has wafers linked to it.");
            }

            await _supplierRepo.DeleteAsync(supplierId);

            await _log.LogInfoAsync(
                $"Supplier ID {supplierId} deleted");

            return (true, "Supplier deleted successfully.");
        }
        catch (Exception ex)
        {
            await _log.LogErrorAsync(
                $"Delete supplier error: {ex.Message}");

            return (false, "Unexpected error occurred.");
        }
    }

}
