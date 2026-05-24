using System;
using System.Collections.Generic;
using System.Text;
using LTS.Common.Constants;
using LTS.Common.Interfaces;
using LTS.Common.Models;

namespace LTS.Application.Services;

public class ProcessLocationService
{
    private readonly IProcessLocationRepository _locationRepo;
    private readonly ILotRepository _lotRepo;
    private readonly ILotRouteRepository _routeRepo;
    private readonly ICarrierRepository _carrierRepo;
    private readonly LogService _logService;

    public ProcessLocationService(
        IProcessLocationRepository locationRepo,
        ILotRepository lotRepo,
        ILotRouteRepository routeRepo,
        ICarrierRepository carrierRepo,
        LogService logService
        )
    {
        _locationRepo = locationRepo;
        _lotRepo = lotRepo;
        _routeRepo = routeRepo;
        _carrierRepo = carrierRepo;
        _logService = logService;
    }

    //get all locations
    public async Task<IEnumerable<ProcessLocation>> GetAllAsync()
       => await _locationRepo.GetAllAsync();

    public async Task<(bool Success, string Message)> SaveRouteAsync(int lotId, List<int> locationIds)
    {
        try
        {
            if (locationIds == null || !locationIds.Any())
                return (false, "No locations selected.");

            await _routeRepo.DeleteByLotIdAsync(lotId);

            for (int i = 0; i < locationIds.Count; i++)
            {
                await _routeRepo.AddAsync(new LotRoute
                {
                    LotId = lotId,
                    ProcessLocationId = locationIds[i],
                    StepOrder = i + 1,
                    IsCompleted = false
                });
            }

            await _logService.LogInfoAsync(
                $"Route saved for lot {lotId}");

            return (true, "Route saved successfully.");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync(ex.Message);

            return (false,
                "Failed to save route.");
        }
    }


    public async Task<(bool Success, string Message)>
    StartLotAsync(int lotId)
    {
        try
        {
            var lot = await _lotRepo.GetByIdAsync(lotId);

            if (lot == null)
                return (false, "Lot not found.");

            if (lot.LotStatus != LotStatus.Idle)
                return (false,
                    "Only Idle lots can be started.");

            var route = (await _routeRepo
                .GetByLotIdAsync(lotId)).ToList();

            if (!route.Any())
                return (false,
                    "No route defined. Please select locations before starting.");

            var firstStep = route.First();

            var location = await _locationRepo
                .GetByIdAsync(firstStep.ProcessLocationId);

            if (location == null)
                return (false,
                    "First location not found.");

            if (location.Status ==
                LocationStatus.Occupied)
                return (false,
                    $"{location.Name} is occupied.");

            // assign location
            location.CurrentLotId = lotId;
            location.Status = LocationStatus.Occupied;

            await _locationRepo
                .UpdateAsync(location);

            // update lot
            lot.LotStatus =
                LotStatus.Processing;

            lot.CurrentProcessLocationId =
                location.ProcessLocationId;

            lot.StartedOn =
                DateTime.Now;

            await _lotRepo.UpdateAsync(lot);

            await _logService.LogInfoAsync(
                $"Lot '{lot.LotName}' started at {location.Name}");

            return (true,
                $"Lot started at {location.Name}");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync(
                $"StartLot failed: {ex.Message}");

            return (false,
                "Unexpected error while starting lot.");
        }
    }

    public async Task<(bool Success, string Message)> MoveToNextAsync(int lotId)
    {
        try
        {
            var lot = await _lotRepo.GetByIdAsync(lotId);
            if (lot == null)
                return (false, "Lot not found.");

            if (lot.LotStatus != LotStatus.Processing)
                return (false, "Lot is not processing.");

            var route = (await _routeRepo.GetByLotIdAsync(lotId)).ToList();

            // find current active step
            var currentStep = route.FirstOrDefault(r => !r.IsCompleted);
            if (currentStep == null)
                return (false, "No active route step found.");

            // find next step BEFORE doing anything
            var nextStep = route
                .Where(r => !r.IsCompleted && r.LotRouteId != currentStep.LotRouteId)
                .OrderBy(r => r.StepOrder)
                .FirstOrDefault();

            // if next step exists, check if that location is occupied FIRST
            // before making any changes
            if (nextStep != null)
            {
                var nextLocation = await _locationRepo
                    .GetByIdAsync(nextStep.ProcessLocationId);

                if (nextLocation == null)
                    return (false, "Next location not found.");

                //check loc occupied 
                if (nextLocation.Status == LocationStatus.Occupied) {

                    bool isDeadlock = await IsDeadlockAsync(
                            lotId, nextLocation.ProcessLocationId);

                    if(isDeadlock)
                    {
                        await _logService.LogWarnAsync(
                            $"Deadlock detected for lot {lotId}");

                        // automatically resolve deadlock
                        return await AutoResolveDeadlockAsync(
                                lotId,
                                nextLocation.CurrentLotId!.Value);
                    }

                    await _logService.LogWarnAsync(
                        $"{nextLocation.Name} is occupied. " +
                        $"Cannot move lot '{lot.LotName}'. Please wait.");

                    return (false,
                            $"{nextLocation.Name} is occupied. " +
                            $"Cannot move lot '{lot.LotName}'. Please wait.");
                }
                

                // safe to move — free current location first
                currentStep.IsCompleted = true;
                await _routeRepo.UpdateAsync(currentStep);

                var currentLocation = await _locationRepo
                    .GetByIdAsync(currentStep.ProcessLocationId);

                if (currentLocation != null)
                {
                    currentLocation.Status = LocationStatus.Available;
                    currentLocation.CurrentLotId = null;
                    await _locationRepo.UpdateAsync(currentLocation); 
                }

                // occupy next location
                nextLocation.CurrentLotId = lotId;
                nextLocation.Status = LocationStatus.Occupied;
                await _locationRepo.UpdateAsync(nextLocation);

                // update lot
                lot.CurrentProcessLocationId = nextLocation.ProcessLocationId;
                await _lotRepo.UpdateAsync(lot);

                await _logService.LogInfoAsync(
                    $"Lot '{lot.LotName}' moved to {nextLocation.Name}.");

                return (true, $"Moved to {nextLocation.Name}.");
            }
            else
            {
                // no next step — this is the last location, complete the lot

                // free current location
                currentStep.IsCompleted = true;
                await _routeRepo.UpdateAsync(currentStep);

                var currentLocation = await _locationRepo
                    .GetByIdAsync(currentStep.ProcessLocationId);

                if (currentLocation != null)
                {
                    currentLocation.Status = LocationStatus.Available;
                    currentLocation.CurrentLotId = null;
                    await _locationRepo.UpdateAsync(currentLocation);
                }

                // complete the lot
                lot.LotStatus = LotStatus.Completed;
                lot.CurrentProcessLocationId = null;
                lot.CompletedOn = DateTime.Now;
                await _lotRepo.UpdateAsync(lot);

                // free the carrier
                if (lot.CarrierId > 0)
                {
                    var carrier = await _carrierRepo.GetByIdAsync(lot.CarrierId);
                    if (carrier != null)
                    {
                        carrier.Status = "Unallocated";
                        await _carrierRepo.UpdateAsync(carrier);
                        await _logService.LogInfoAsync(
                            $"Carrier '{carrier.CarrierCode}' released to Unallocated.");
                    }
                }

                await _logService.LogInfoAsync(
                    $"Lot '{lot.LotName}' completed all route steps.");

                return (true, $"Lot '{lot.LotName}' completed!");
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync(
                $"MoveNext failed: {ex.Message}");

            return (false, "Unexpected error while moving lot.");
        }
    }

    // Peek next process location for animation/UI purposes  Does NOT move the lot.  Only returns where the lot will move next.
    public async Task<int?> PeekNextLocationIdAsync(int lotId)
    {
        var route = (await _routeRepo
            .GetByLotIdAsync(lotId))
            .ToList();

        var currentStep = route
            .FirstOrDefault(r => !r.IsCompleted);

        if (currentStep == null)
            return null;

        var nextStep = route
            .Where(r =>
                !r.IsCompleted &&
                r.LotRouteId != currentStep.LotRouteId)
            .OrderBy(r => r.StepOrder)
            .FirstOrDefault();

        return nextStep?.ProcessLocationId;
    }

    //check if the lot occupying the next location is ALSO waiting for YOUR current location. check circular wait

    private async Task<bool> IsDeadlockAsync(
     int currentLotId, int blockedByLocationId)
    {
        try
        {
            // find which lot is at the blocking location
            // example:
            // current lot wants Location 2
            // so blockedByLocationId = 2
            var blockingLocation = await _locationRepo
                .GetByIdAsync(blockedByLocationId);

            if (blockingLocation?.CurrentLotId == null)
                return false;

            // get the lot occupying that location
            // example:
            // Lot B currently sitting at Location 2
            int blockingLotId = blockingLocation.CurrentLotId.Value;

            // get blocking lot's route
            var blockingRoute = (await _routeRepo
                .GetByLotIdAsync(blockingLotId)).ToList();

            // find current active step of blocking lot
            // active step = first unfinished route step
            var blockingCurrentStep = blockingRoute
                .FirstOrDefault(r => !r.IsCompleted);

            if (blockingCurrentStep == null)
                return false;

            // find next step of blocking lot
            // meaning:
            // where does blocking lot want to move next?
            var blockingNextStep = blockingRoute
                .Where(r => !r.IsCompleted
                       && r.StepOrder > blockingCurrentStep.StepOrder)
                .OrderBy(r => r.StepOrder)
                .FirstOrDefault();

            // no next step = no deadlock
            if (blockingNextStep == null)
                return false;

            // find current location of current lot
            // example:
            // Lot A currently sitting at Location 1
            var currentLotLocation = (await _locationRepo
                .GetAllAsync())
                .FirstOrDefault(l => l.CurrentLotId == currentLotId);

            // Lot A at L1 wants L2
            // Lot B at L2 wants L1
            //
            // B's next step = L1
            // current lot location = L1
            //
            // TRUE → deadlock detected
            return blockingNextStep.ProcessLocationId
                == currentLotLocation?.ProcessLocationId;
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync(
                $"Deadlock detection failed: {ex.Message}");

            return false;
        }
    }


    private async Task<(bool Sucess, String Message)>  AutoResolveDeadlockAsync(int currentLotId, int blockingLotId)
    {
        try
        {
            var currentLot = await _lotRepo.GetByIdAsync(currentLotId);
            var blockingLot = await _lotRepo.GetByIdAsync(blockingLotId);

            if (currentLot == null || blockingLot == null)
                return (false, "Lot not found during deadlock resolution.");

            // decide priority — earlier StartedOn wins

            bool currentHasPriority =
                currentLot.StartedOn <= blockingLot.StartedOn;

            var priorityLotId =
                currentHasPriority ? currentLotId : blockingLotId;

            var waitingLotId =
                currentHasPriority ? blockingLotId : currentLotId;


            await _logService.LogWarnAsync(
                    $"Deadlock auto-resolve: " +
                    $"Lot {priorityLotId} moves first, " +
                    $"Lot {waitingLotId} holds.");


            var waitingLot =
                await _lotRepo.GetByIdAsync(waitingLotId);

            if (waitingLot == null)
                return (false, "Waiting lot not found");

            // free the location the waiting lot is sitting at

            var waitingLocation = (await _locationRepo.GetAllAsync())
            .FirstOrDefault(l => l.CurrentLotId == waitingLotId);


            // free that location
            if (waitingLocation != null)
            {
                // mark location free
                waitingLocation.Status = LocationStatus.Available;

                // remove lot from location
                waitingLocation.CurrentLotId = null;

                // save location update
                await _locationRepo.UpdateAsync(waitingLocation);
            }

            // put waiting lot into Queued — not at any location
            waitingLot.LotStatus = LotStatus.Queued;

            // lot not sitting anywhere now
            waitingLot.CurrentProcessLocationId = null;

            // save lot update
            await _lotRepo.UpdateAsync(waitingLot);

            await _logService.LogWarnAsync(
                $"Lot '{waitingLot.LotName}' moved to Queued — " +
                $"holding for deadlock resolution.");

            // Step 2 — now move priority lot forward normally
            var result = await MoveToNextAsync(priorityLotId);

            await _logService.LogInfoAsync(
           $"Priority lot '{priorityLotId}' moved: {result.Message}");

            // Step 3 — try to restart waiting lot from its current route step
            var resumeResult =
                await ResumeQueuedLotAsync(waitingLotId);

            await _logService.LogInfoAsync(
            $"Waiting lot '{waitingLotId}' resume: {resumeResult.Message}");

            return (true,
                    $"Deadlock resolved. " +
                    $"Lot {priorityLotId} moved first. " +
                    $"Lot {waitingLotId} queued for retry.");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync(
                $"Deadlock auto-resolve failed: {ex.Message}");

            return (false,
                "Unexpected error during deadlock resolution.");
        }
    }
    // resumes a Queued lot back into Processing
    public async Task<(bool Success, string Message)>
     ResumeQueuedLotAsync(int lotId)
    {
        try
        {
            var lot = await _lotRepo.GetByIdAsync(lotId);

            if (lot == null)
                return (false, "Lot not found.");

            // only queued lots can resume
            if (lot.LotStatus != LotStatus.Queued)
                return (false, "Lot is not in Queued state.");



            // get full route of lot
            var route = (await _routeRepo
                .GetByLotIdAsync(lotId)).ToList();



            // find next unfinished step
            // this is where lot should continue
            var nextStep = route
                .Where(r => !r.IsCompleted)
                .OrderBy(r => r.StepOrder)
                .FirstOrDefault();



            // no next step = production finished
            if (nextStep == null)
            {
                // complete lot
                lot.LotStatus = LotStatus.Completed;

                // save completion time
                lot.CompletedOn = DateTime.Now;

                // save lot update
                await _lotRepo.UpdateAsync(lot);

                return (true, "Lot completed during resume.");
            }



            // get next target location
            var nextLocation = await _locationRepo
                .GetByIdAsync(nextStep.ProcessLocationId);

            if (nextLocation == null)
                return (false, "Next location not found.");



            // if still occupied,
            // keep lot in queued state
            if (nextLocation.Status == LocationStatus.Occupied)
            {
                await _logService.LogWarnAsync(
                    $"Lot '{lot.LotName}' still blocked at resume — staying Queued.");

                return (false,
                    $"Still blocked at {nextLocation.Name}. Will retry.");
            }



            // location available → resume production

            // assign lot into location
            nextLocation.CurrentLotId = lotId;

            // mark location occupied
            nextLocation.Status = LocationStatus.Occupied;

            // save location update
            await _locationRepo.UpdateAsync(nextLocation);



            // move lot back into processing state
            lot.LotStatus = LotStatus.Processing;

            // update current location of lot
            lot.CurrentProcessLocationId =
                nextLocation.ProcessLocationId;

            // save lot update
            await _lotRepo.UpdateAsync(lot);



            await _logService.LogInfoAsync(
                $"Lot '{lot.LotName}' resumed at {nextLocation.Name}.");

            return (true,
                $"Lot resumed at {nextLocation.Name}.");
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync(
                $"Resume queued lot failed: {ex.Message}");

            return (false,
                "Unexpected error while resuming queued lot.");
        }
    }

}