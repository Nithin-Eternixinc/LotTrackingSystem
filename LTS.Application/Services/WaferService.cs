using System;
using System.Collections.Generic;
using System.Text;
using LTS.Common.Interfaces;
using LTS.Common.Models;

namespace LTS.Application.Services;

public class WaferService
{
    private readonly IWaferRepository _waferRepo;
    private readonly LogService _log;

    public WaferService(IWaferRepository waferRepo, LogService log)
    {
        _waferRepo = waferRepo;
        _log = log;
    }

    // Get all wafers
    public async Task<IEnumerable<WaferMaster>> GetAllAsync()
    {
        return await _waferRepo.GetAllAsync();
    }

    public async Task<IEnumerable<WaferMaster>> GetUnallocatedAsync()
    {
        return await _waferRepo.GetUnallocatedAsync();
    }

    public async Task<int> GetUnallocatedCountAsync()
    {
        return await _waferRepo.GetUnallocatedCountAsync();
    }

    //Add Wafer
    public async Task<(bool Success, string Message)> AddWaferAsync(WaferMaster wafer)
    {
        try
        {
            //validation
            if (string.IsNullOrWhiteSpace(wafer.WaferSerialNo))
            {
                await _log.LogWarnAsync("Wafer creation failed - SerilaNo missing");
                return (false, "Wafer serial number is required.");
            }

            if (wafer.SupplierId == 0)
                return (false, "Please select a supplier.");

            //duplicate check
            if (await _waferRepo.ExistsBySerialAsync(wafer.WaferSerialNo))
            {
                await _log.LogWarnAsync(
               $"Duplicate wafer serial: {wafer.WaferSerialNo}");

                return (false,
                    $"Wafer '{wafer.WaferSerialNo}' already exists.");
            }

            wafer.CreatedOn = DateTime.Now;
            wafer.WaferStatus = "Unallocated";

            await _waferRepo.AddAsync(wafer); //save

            // Log success
            await _log.LogInfoAsync(
                $"Wafer '{wafer.WaferSerialNo}' added successfully");

            return (true, "Wafer added successfully.");


        }
        catch(Exception ex)
        {
            await _log.LogErrorAsync(ex.Message);

            return (false,
                "Unexpected error occurred.");
        }
    }

    public async Task<(bool Success, string Message)>
    DeleteWaferAsync(int waferId)
    {
        try
        {
            // Prevent delete if allocated
            if (await _waferRepo.IsAllocatedAsync(waferId))
            {
                await _log.LogWarnAsync(
                    $"Delete blocked — wafer ID {waferId} is allocated");

                return (false,
                    "Cannot delete allocated wafer.");
            }

            await _waferRepo.DeleteAsync(waferId);

            await _log.LogInfoAsync(
                $"Wafer ID {waferId} deleted");

            return (true,
                "Wafer deleted successfully.");
        }
        catch (Exception ex)
        {
            await _log.LogErrorAsync(
                $"Delete failed for wafer ID {waferId}: {ex.Message}");

            return (false,
                "Unexpected error occurred while deleting wafer.");
        }
    }



}
