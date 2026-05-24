using LTS.Application.Services;
using LTS.Common.Constants;
using LTS.Common.Interfaces;
using LTS.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTS.Application.Repositories;

public class LotService
{
    private readonly ILotRepository _lotRepo;
    private readonly IWaferRepository _waferRepo;
    private readonly ILotRouteRepository _routeRepo;
    private readonly ICarrierRepository _carrierRepo;
    private readonly LogService _logService;

    public LotService(ILotRepository lotRepo,
                      IWaferRepository waferRepo,
                      ILotRouteRepository routeRepo,
                      ICarrierRepository carrierRepo,
                      LogService logService)
    {
        _lotRepo = lotRepo;
        _waferRepo = waferRepo;
        _routeRepo = routeRepo;
        _carrierRepo = carrierRepo;
        _logService = logService;
    }

    public async Task<IEnumerable<Lot>> GetAllLotsAsync()
    {
        return await _lotRepo.GetAllAsync();
    }

    public async Task<(bool Success, string Message)> CreateLotAsync(
    string lotName,
    int carrierId,
    List<int> selectedWaferIds)
    {
        try
        {
            // validation
            if (string.IsNullOrWhiteSpace(lotName))
            {
                await _logService.LogWarnAsync("Lot creation failed — lot name missing.");
                return (false, "Lot name is required.");
            }

            if (carrierId <=0)
            {
                await _logService.LogWarnAsync("Lot creation failed — carrier not selected.");
                return (false, "Please select a carrier.");
            }

            if (selectedWaferIds == null || selectedWaferIds.Count == 0)
            {
                await _logService.LogWarnAsync("Lot creation failed — no wafers selected.");
                return (false, "Please select at least one wafer.");
            }

            // get carrier
            var carrier = (await _carrierRepo.GetAllAsync())
                .FirstOrDefault(c => c.CarrierId == carrierId);

            if (carrier == null)
            {
                await _logService.LogErrorAsync($"Lot creation failed — carrier ID {carrierId} not found.");
                return (false, "Carrier not found.");
            }

            // carrier already occupied
            if (carrier.Status == "Allocated")
            {
                await _logService.LogWarnAsync(
                    $"Lot creation blocked — carrier '{carrier.CarrierCode}' already allocated.");

                return (false, "Selected carrier is already in use.");
            }

            // capacity validation
            if (selectedWaferIds.Count > carrier.Capacity)
            {
                await _logService.LogWarnAsync(
                    $"Lot creation blocked — wafer count exceeded carrier capacity.");

                return (false,
                    $"Selected wafers ({selectedWaferIds.Count}) exceed carrier capacity ({carrier.Capacity}).");
            }

            // create lot
            var lot = new Lot
            {
                LotName = lotName.Trim(),
                CarrierId = carrierId,
                WaferCount = selectedWaferIds.Count,
                LotStatus = LotStatus.Idle,
                CreatedOn = DateTime.Now
            };

            await _lotRepo.AddAsync(lot);

            // allocate wafers
            foreach (var waferId in selectedWaferIds)
            {
                var wafer = await _waferRepo.GetByIdAsync(waferId);

                if (wafer != null)
                {
                    wafer.LotId = lot.LotId;
                    wafer.WaferStatus = "Allocated";

                    await _waferRepo.UpdateAsync(wafer);
                }
            }

            // allocate carrier
            carrier.Status = "Allocated";
            await _carrierRepo.UpdateAsync(carrier);

            await _logService.LogInfoAsync(
                $"Lot '{lot.LotName}' created successfully with {selectedWaferIds.Count} wafers.");

            return (true, "Lot created successfully.");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync(
                $"Exception while creating lot '{lotName}' : {ex.Message}");

            return (false, "Unexpected error occurred while creating lot.");
        }
    }

    

    public async Task<(bool Success, string Message)> DeleteLotAsync(int lotId)
    {
        try
        {
            var lot = await _lotRepo.GetByIdAsync(lotId);

            if (lot == null)
            {
                await _logService.LogWarnAsync(
                    $"Delete failed — lot ID {lotId} not found.");

                return (false, "Lot not found.");
            }

            // block deleting processing lots
            if (lot.LotStatus == LotStatus.Processing)
            {
                await _logService.LogWarnAsync(
                    $"Delete blocked — lot '{lot.LotName}' is currently Processing.");

                return (false, "Cannot delete a lot that is currently processing.");
            }

            // only idle allowed
            if (lot.LotStatus != LotStatus.Idle)
            {
                await _logService.LogWarnAsync(
                    $"Delete blocked — lot '{lot.LotName}' status is {lot.LotStatus}.");

                return (false,
                    $"Only Idle lots can be deleted. Current status: {lot.LotStatus}");
            }

            // free wafers
            var wafers = (await _waferRepo.GetAllAsync())
                .Where(w => w.LotId == lotId)
                .ToList();

            foreach (var wafer in wafers)
            {
                string autoPrefix = $"waf-{lot.LotName}-";
                if (wafer.WaferSerialNo.StartsWith(autoPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    //delet autogenerated wafer
                    await _waferRepo.DeleteAsync(wafer.WaferId);
                }
                else
                {
                    //manually added wafer -just free it 
                    wafer.LotId = null;
                    wafer.WaferStatus = "Unallocated";

                    await _waferRepo.UpdateAsync(wafer);
                }

            }

            // free carrier
            if (lot.Carrier != null)
            {
                lot.Carrier.Status = "Unallocated";
    

                await _carrierRepo.UpdateAsync(lot.Carrier);
            }

            // delete lot
            await _lotRepo.DeleteAsync(lotId);

            await _logService.LogInfoAsync(
                $"Lot '{lot.LotName}' deleted successfully.");

            return (true, "Lot deleted successfully.");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync(
                $"Exception while deleting lot ID {lotId} : {ex.Message}");

            return (false, "Unexpected error occurred while deleting lot.");
        }
    }
    public async Task<IEnumerable<LotRoute>> GetRouteForLotAsync(int lotId)
    {
        return await _routeRepo.GetByLotIdAsync(lotId);
    }

    public async Task <(bool Success , string Message ,List<int> WaferIds)> FillWafersAsync(string lotName
        ,int carrierId, int supplierId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(lotName))
                return (false, "Lot name is required before filling wafers.", new());

            if (string.IsNullOrWhiteSpace(lotName))
                return (false, "Lot name is required before filling wafers.", new());

            if (supplierId <= 0)
                return (false, "Please select a supplier.", new());

            // get carrier capacity
            var carrier = (await _carrierRepo.GetAllAsync())
                .FirstOrDefault(c => c.CarrierId == carrierId);

            if (carrier == null)
                return (false, "Carrier not found.", new());

            if (carrier.Status == "Allocated")
                return (false, "Selected carrier is already in use.", new());

            int count = carrier.Capacity; // always fill to capacity

            var createdIds = new List<int>();

            for (int slot =1; slot<= count; slot++)
            {
                string serial = $"waf-{lotName.Trim()}-{slot}";
                // skip if serial already exists
                if (await _waferRepo.ExistsBySerialAsync(serial))
                {
                    await _logService.LogWarnAsync(
                        $"Wafer serial '{serial}' already exists — skipped.");
                    continue;
                }

                var wafer = new WaferMaster
                {
                    WaferSerialNo = serial,
                    SupplierId = supplierId,
                    WaferStatus = "Unallocated",
                    CreatedOn = DateTime.Now
                };

                await _waferRepo.AddAsync(wafer);
                createdIds.Add(wafer.WaferId);
            }

            if (!createdIds.Any())
                return (false,
                    "No wafers created — all serial numbers already exist.",
                    new());

            await _logService.LogInfoAsync(
            $"Fill Wafers: {createdIds.Count} wafers created for lot '{lotName}'.");


            return (true,
            $"{createdIds.Count} wafers created and ready to assign.",
            createdIds);
        }

        catch (Exception ex)
        {
            await _logService.LogErrorAsync(
            $"FillWafers failed: {ex.Message}");

            return (false, "Unexpected error while filling wafers.", new ());
        }
        }

}

