using System;
using System.Collections.Generic;
using System.Text;
using LTS.Application.Data;
using LTS.Common.Interfaces;
using LTS.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace LTS.Application.Services;

public class CarrierService
{
    private readonly ICarrierRepository _carrierRepo;
    private readonly LogService _log;
    public CarrierService(ICarrierRepository carrierRepo , LogService log)
    {
        _carrierRepo = carrierRepo;
        _log = log;
    }

    public async Task<IEnumerable<Carrier>> GetAllCarriersAsync()
    {
        return await _carrierRepo.GetAllAsync();
    }

    public async Task<IEnumerable<Carrier>> GetAvailableCarriersAsync()
    {
        return await _carrierRepo.GetAvailableAsync();
    }


    public async Task<(bool Success, string Message)> AddCarrierAsync(string code, int capacity)
    {
        try
        {
            // validation
            if (string.IsNullOrWhiteSpace(code))
            {
                await _log.LogWarnAsync("Carrier creation failed — empty carrier code");
                return (false, "Carrier code is required.");
            }

            if (capacity <= 0 || capacity > 25)
            {
                await _log.LogWarnAsync(
                    $"Carrier creation failed — invalid capacity {capacity}"
                );

                return (false, "Capacity must be between 1 and 25.");
            }

            // duplicate check
            if (await _carrierRepo.ExistsByCodeAsync(code))
            {
                await _log.LogWarnAsync(
                    $"Duplicate carrier code attempted: {code}"
                );

                return (false, $"Carrier '{code}' already exists.");
            }

            // create carrier
            var carrier = new Carrier
            {
                CarrierCode = code.Trim(),
                Capacity = capacity,
                Status = "Unallocated",
                CurrentLocation = "None",
                CreatedOn = DateTime.Now
            };

            await _carrierRepo.AddAsync(carrier);

            await _log.LogInfoAsync(
                $"Carrier '{carrier.CarrierCode}' created successfully with capacity {carrier.Capacity}"
            );

            return (true, "Carrier added successfully.");
        }
        catch (Exception ex)
        {
            await _log.LogErrorAsync(
                $"Exception while creating carrier '{code}' : {ex.Message}"
            );

            return (false, "Unexpected error occurred while adding carrier.");
        }
    }


    //delet carreir

    public async Task<(bool Success, string Message)> DeleteCarrierAsync(int carrierId)
    {
        try
        {
            var carrier = (await _carrierRepo.GetAllAsync())
                .FirstOrDefault(c => c.CarrierId == carrierId);

            if (carrier == null)
            {
                await _log.LogWarnAsync(
                    $"Delete failed — carrier ID {carrierId} not found"
                );

                return (false, "Carrier not found.");
            }

            // block delete if carrier is allocated
            if (carrier.Status == "Allocated")
            {
                await _log.LogWarnAsync(
                    $"Delete blocked — carrier '{carrier.CarrierCode}' is allocated"
                );

                return (false, "Cannot delete an allocated carrier.");
            }

            await _carrierRepo.DeleteAsync(carrierId);

            await _log.LogInfoAsync(
                $"Carrier '{carrier.CarrierCode}' deleted successfully"
            );

            return (true, "Carrier deleted successfully.");
        }
        catch (Exception ex)
        {
            await _log.LogErrorAsync(
                $"Exception while deleting carrier ID {carrierId} : {ex.Message}"
            );

            return (false, "Unexpected error occurred while deleting carrier.");
        }
    }



}
