using LTS.Application.Repositories;
using LTS.Application.Services;
using LTS.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Media.Animation;

namespace LTS.UI.ViewModels;

public class LotViewModel : ViewModelBase
{
    private readonly LotService _lotService;
    private readonly WaferService _waferService;
    private readonly CarrierService _carrierService;
    private readonly SupplierService _supplierService;

    public ObservableCollection<SelectableWafer> AvailableWafers { get; set; } = new(); //store data for your UI.
    public ObservableCollection<Carrier> AvailableCarriers { get; set; } = new();
    public ObservableCollection<Lot> Lots { get; set; } = new();
    public ObservableCollection<Supplier> Suppliers { get; set; } = new();

    // --- LotName ---
    private string _lotName = "";
    public string LotName
    {
        get => _lotName;
        set
        {
            SetProperty(ref _lotName, value);
            CreateLotCommand.RaiseCanExecuteChanged();
            FillWafersCommand.RaiseCanExecuteChanged();
        }
    }

    // --- SelectedCarrier ---
    private Carrier? _selectedCarrier;
    public Carrier? SelectedCarrier
    {
        get => _selectedCarrier;
        set
        {
            SetProperty(ref _selectedCarrier, value);
            CreateLotCommand.RaiseCanExecuteChanged();
            FillWafersCommand.RaiseCanExecuteChanged();
        }
    }

    // --- SelectedLot for delete---
    private Lot? _selectedLot;
    public Lot? SelectedLot
    {
        get => _selectedLot;
        set
        {
            SetProperty(ref _selectedLot, value);
            DeleteLotCommand.RaiseCanExecuteChanged();
        }
    }

    private int _selectedSupplierId;
    public int SelectedSupplierId
    {
        get => _selectedSupplierId;
        set
        {
            SetProperty(ref _selectedSupplierId, value);
            FillWafersCommand.RaiseCanExecuteChanged();
        }
    }



    public AsyncRelayCommand CreateLotCommand { get; }
    public AsyncRelayCommand DeleteLotCommand { get; }
    public AsyncRelayCommand FillWafersCommand { get; }

    public LotViewModel(LotService lotService,
                    WaferService waferService,
                    CarrierService carrierService,
                    SupplierService supplierService)
    {
        _lotService = lotService;
        _waferService = waferService;
        _carrierService = carrierService;
        _supplierService = supplierService;

        CreateLotCommand = new AsyncRelayCommand(
            execute: CreateLotAsync,
            canExecute: () =>
                !string.IsNullOrWhiteSpace(LotName)
                && SelectedCarrier != null
                && AvailableWafers.Any(w => w.IsSelected)
        );

        DeleteLotCommand = new AsyncRelayCommand(
            execute: DeleteLotAsync,
            canExecute: () => SelectedLot != null
        );

        FillWafersCommand = new AsyncRelayCommand(
            execute: FillWafersAsync,
            canExecute: () =>
            !string.IsNullOrWhiteSpace(LotName)
            && SelectedCarrier != null
            && SelectedSupplierId > 0);


        _ = LoadAllAsync();
        _ = LoadSuppliersAsync();
    }

    private async Task LoadAllAsync()
    {
        await LoadAvailableCarriersAsync();
        await LoadAvailableWafersAsync();
        await LoadLotsAsync();
    }

    private async Task LoadSuppliersAsync()
    {
        var list = await _supplierService.GetAllAsync();
        Suppliers.Clear();
        foreach (var s in list) Suppliers.Add(s);
    }

    private async Task FillWafersAsync()
    {
        if (SelectedCarrier == null) return;

        var result = await _lotService.FillWafersAsync(
            LotName,
            SelectedCarrier.CarrierId,
            SelectedSupplierId
            );

        if (!result.Success)
        {
            MessageBox.Show(result.Message, "Fill wafers failed",
                     MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        await LoadAvailableWafersAsync();
        // auto-select all newly created wafers
        foreach (var sw in AvailableWafers)
        {
            if (result.WaferIds.Contains(sw.Wafer.WaferId))
                sw.IsSelected = true;
        }
        MessageBox.Show(result.Message, "Fill Wafers",
        MessageBoxButton.OK, MessageBoxImage.Information);

    }






    private async Task LoadAvailableCarriersAsync()
    {
        var list = await _carrierService.GetAvailableCarriersAsync();
        AvailableCarriers.Clear();
        foreach (var carrier in list)
            AvailableCarriers.Add(carrier);
    }

    private async Task LoadAvailableWafersAsync()
    {
        var list = await _waferService.GetUnallocatedAsync();
        AvailableWafers.Clear();
        foreach (var wafer in list)
        {
            var selectable = new SelectableWafer(wafer);
            // notify CreateLotCommand when checkbox changes
            selectable.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectableWafer.IsSelected))
                    CreateLotCommand.RaiseCanExecuteChanged();
            };
            AvailableWafers.Add(selectable);
        }


    }

    private async Task LoadLotsAsync()
    {
        var list = await _lotService.GetAllLotsAsync();
        Lots.Clear();
        foreach (var lot in list)
            Lots.Add(lot);
    }

    private async Task CreateLotAsync()
    {
        var selectedWaferIds = AvailableWafers
            .Where(w => w.IsSelected)
            .Select(w => w.Wafer.WaferId)
            .ToList();

        var result = await _lotService.CreateLotAsync(
            LotName,
            SelectedCarrier!.CarrierId,
            selectedWaferIds);

        MessageBox.Show(result.Message);

        if (result.Success)
        {
            LotName = "";
            SelectedCarrier = null;
            await LoadAllAsync(); 
        }
    }

    private async Task DeleteLotAsync()
    {
        if (SelectedLot == null) return;

        var result = await _lotService.DeleteLotAsync(SelectedLot.LotId);

        MessageBox.Show(result.Message);

        if (result.Success)
        {
            Lots.Remove(SelectedLot);
            await LoadAllAsync(); 
        }
    }

}