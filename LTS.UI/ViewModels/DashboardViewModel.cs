using LTS.Application.Repositories;
using LTS.Application.Services;
using LTS.Common.Constants;
using LTS.Common.Models;
using LTS.UI.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows;


namespace LTS.UI.ViewModels;

public class DashboardViewModel : ViewModelBase 
{

    private readonly LotService _lotService;
    private readonly WaferService _waferService;
    private readonly ProcessLocationService _locationService;
    private readonly LogService _logService;
    private readonly System.Windows.Threading.DispatcherTimer _scheduler;    // UI-friendly timer that lets you run code at regular intervals and directly update WPF controls

    public event Func<int, int, Task>? AnimateMoveRequested;

    private int _availableWafers;
   
    public int AvailableWafers{
        get => _availableWafers;
        set => SetProperty(ref _availableWafers, value);
    }

    private int _idleLots;

    public int IdleLots
    {
        get => _idleLots;
        set => SetProperty(ref _idleLots, value);
    }


    private int _freeLocations;

    public int FreeLocations
    {
        get => _freeLocations;
        set => SetProperty(ref _freeLocations, value);

    }

    //property controls whether your system is in Auto mode or Manual mode.

    private bool _isAutoMode;
    public bool IsAutoMode
    {
        get => _isAutoMode;
        set
        {
            SetProperty(ref _isAutoMode, value);
            OnPropertyChanged(nameof(IsManualMode)); //This property changed. Refresh UI
            OnPropertyChanged(nameof(ShowMoveNext));
            ShowMoveNext = !value;
            ToggleSchedulerCommand.RaiseCanExecuteChanged(); //Check again if button should be enabled or disabled
            if (!value) StopScheduler();
        }
    }

    public bool IsManualMode => !_isAutoMode;  //Return opposite of _isAutoMode

    private int _intervalSeconds = 10;
    public int IntervalSeconds
    {
        get => _intervalSeconds;
        set
        {
            if (value < 1) value = 1; // minimum 1 second
            SetProperty(ref _intervalSeconds, value);
            // update timer interval if running
            if (_scheduler.IsEnabled)
            {
                _scheduler.Interval = TimeSpan.FromSeconds(value);
            }
        }
    }

    private bool _schedulerRunning;
    public bool SchedulerRunning
    {
        get => _schedulerRunning;
        set
        {
            SetProperty(ref _schedulerRunning, value);
            ToggleSchedulerCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(SchedulerButtonText));
        }
    }

    public string SchedulerButtonText
        => _schedulerRunning ? "⏹ Stop Auto" : "▶ Start Auto";

    // move next button only visible in manual mode
    //public bool ShowMoveNext => IsManualMode;

    private bool _showMoveNext = true;
    public bool ShowMoveNext
    {
        get => _showMoveNext;
        set => SetProperty(ref _showMoveNext, value);
    }

    //lot table
    public ObservableCollection<Lot> Lots { get; set; } = new();

    //location grid
    public ObservableCollection<ProcessLocation> Locations { get; set; } = new();

    public ObservableCollection<ApplicationLog> Logs { get; } = new();

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);

    }

    private Lot? _selectedLot;
    public Lot? SelectedLot
    {
        get => _selectedLot;
        set
        {
            SetProperty(ref _selectedLot, value);
            StartCommand.RaiseCanExecuteChanged();
            MoveNextCommand.RaiseCanExecuteChanged();
        }
    }

    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand StartCommand { get; }
    public AsyncRelayCommand MoveNextCommand { get; }
    public RelayCommand ToggleSchedulerCommand { get; }
    public DashboardViewModel(
        LotService lotService,
        WaferService waferService,
        ProcessLocationService locationService,
        LogService logService)
    {
        _lotService = lotService;
        _waferService = waferService;
        _locationService = locationService;
        _logService = logService;

        RefreshCommand = new AsyncRelayCommand(LoadAllAsync);

        StartCommand = new RelayCommand(
            execute: OpenRoutePickerAndStart,
            canExecute: () => SelectedLot?.LotStatus == "Idle"
        );

        MoveNextCommand = new AsyncRelayCommand(
            execute: MoveNextAsync,
            canExecute: () => SelectedLot?.LotStatus == "Processing"
        );


        ToggleSchedulerCommand = new RelayCommand(
            execute: ToggleScheduler
            
         );

        _scheduler = new System.Windows.Threading.DispatcherTimer(); //This creates a WPF timer
        _scheduler.Tick += async (s, e) => await RunSchedulerTickAsync();

        _ = LoadAllAsync();
    }

    //move automatically 
    private async Task RunSchedulerTickAsync()
    {

        
        StatusMessage = $"Tick fired at {DateTime.Now:HH:mm:ss}";


        // get all currently processing lots
        var allLots = await _lotService.GetAllLotsAsync();
        var activeLots = allLots
            .Where(l =>
            l.LotStatus == LotStatus.Processing ||
            l.LotStatus == LotStatus.Queued)
            .OrderBy(l => l.LotStatus == LotStatus.Queued ? 1 : 0).ToList();

        if (!activeLots.Any())
        {
            StatusMessage = "No processing lots — scheduler waiting.";
            await LoadAllAsync();
            return;
        }

        // move each processing lot to its next location
        foreach (var lot in activeLots)
        {
            if (lot.LotStatus == LotStatus.Processing)
            {
                int fromId = lot.CurrentProcessLocationId ?? 0;

                int? toId = await _locationService.PeekNextLocationIdAsync(lot.LotId);

                // animate first
                if (toId != null && AnimateMoveRequested != null)
                {
                    await AnimateMoveRequested.Invoke(fromId, toId.Value);
                }


                var result = await _locationService.MoveToNextAsync(lot.LotId);
                await _logService.LogInfoAsync(
                    $"[Auto] {result.Message}");
            }
            else if (lot.LotStatus == LotStatus.Queued)
            {
                var result = await _locationService
                    .ResumeQueuedLotAsync(lot.LotId);

                await _logService.LogInfoAsync(
                    $"[Auto Resume] {result.Message}");
            }

        }

        StatusMessage = $"[Auto] Tick at {DateTime.Now:HH:mm:ss} — " +
                        $"moved {activeLots.Count} lot(s).";

        await LoadAllAsync();
    }





    private void ToggleScheduler()
    {
        if (!IsAutoMode) return;

        if (_schedulerRunning)
            StopScheduler();
        else
            StartScheduler();
    }

    public void StartScheduler()
    {
        if (!IsAutoMode) return;
        _scheduler.Interval = TimeSpan.FromSeconds(IntervalSeconds);
        _scheduler.Start();
        SchedulerRunning = true;
        StatusMessage = $"Auto scheduler started — moving every {IntervalSeconds}s.";
    }

    public void StopScheduler()
    {
        _scheduler.Stop();
        SchedulerRunning = false;
        StatusMessage = "Auto scheduler stopped.";
    }


    private async Task LoadAllAsync()
    {
        await LoadStatsAsync();
        await LoadLotsAsync();
        await LoadLocationsAsync();
        await LoadLogsAsync();
    }

    private async Task LoadLogsAsync()
    {
        var logs = await _logService.GetAllLogsAsync();

        

        Logs.Clear();

        foreach (var log in logs)
        {
            Logs.Add(log);
        }
    }
   
    public async Task LoadStatsAsync()
    {
        AvailableWafers = await _waferService.GetUnallocatedCountAsync();

        var allLots = await _lotService.GetAllLotsAsync();
        var lotList = allLots.ToList();
        IdleLots = lotList.Count(l => l.LotStatus == "Idle");

        var locations = await _locationService.GetAllAsync();
        FreeLocations = locations.Count(l => l.Status == "Available");

    }

    private async Task LoadLotsAsync()
    {
        var list = await _lotService.GetAllLotsAsync();
        Lots.Clear();
        foreach (var l in list) Lots.Add(l);
    }
    private async Task LoadLocationsAsync()
    {
        var list = await _locationService.GetAllAsync();
        Locations.Clear();
        foreach (var loc in list) Locations.Add(loc);
    }

    private void OpenRoutePickerAndStart()
    {
        if (SelectedLot == null) return;

        int lotId = SelectedLot.LotId;

        var pickerVM = new RoutePickerViewModel(Locations);
        var picker = new RoutePickerWindow(pickerVM)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (picker.ShowDialog() != true) return;

        var selectedIds = pickerVM.GetSelectedLocationIds();
        if (!selectedIds.Any())
        {
            StatusMessage = "No locations selected.";
            return;
        }

        // save route then start 
        _ = SaveRouteAndStartAsync(lotId, selectedIds);
    }


    private async Task SaveRouteAndStartAsync(int lotId, List<int> locationIds)
    {
        // save the route
        await _locationService.SaveRouteAsync(lotId, locationIds);

        // start the lot
        var result = await _locationService.StartLotAsync(lotId);
        StatusMessage = result.Message;

        await LoadAllAsync();

        SelectedLot = Lots.FirstOrDefault(l => l.LotId == lotId);



        StartCommand.RaiseCanExecuteChanged();
        MoveNextCommand.RaiseCanExecuteChanged();
    }

    private async Task MoveNextAsync()
    {
        if (SelectedLot == null) return;
       // string lotName = SelectedLot.LotName;
        int currentLotId = SelectedLot.LotId;
        //current  locat
        int fromId = SelectedLot.CurrentProcessLocationId ?? 0;


        // ask service where lot will move next
        int? toId = await _locationService.PeekNextLocationIdAsync(currentLotId);
        // animate first
        if (toId != null && AnimateMoveRequested != null)
        { await AnimateMoveRequested.Invoke(fromId, toId.Value); }

        // NOW actually move the lot in DB
        var result = await _locationService.MoveToNextAsync(currentLotId);
        StatusMessage = result.Message;

        if (!result.Success) 
        {
            MessageBox.Show(result.Message, "Move Failed",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            await LoadAllAsync();
            SelectedLot = Lots.FirstOrDefault(l => l.LotId == currentLotId);
            return;
        }


        await LoadAllAsync();

        // re-select the same lot after refresh
        SelectedLot = Lots.FirstOrDefault(l => l.LotId == currentLotId);


        StartCommand.RaiseCanExecuteChanged();
        MoveNextCommand.RaiseCanExecuteChanged();
    }



}