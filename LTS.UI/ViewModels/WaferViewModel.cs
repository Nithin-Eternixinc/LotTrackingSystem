using LTS.Application.Services;
using LTS.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Text;

namespace LTS.UI.ViewModels;

public class WaferViewModel : ViewModelBase
{
    private readonly WaferService _waferService;
    private readonly SupplierService _supplierService;


    public ObservableCollection<WaferMaster> Wafers { get; set; } = new();
    public ObservableCollection<Supplier> Suppliers { get; set; } = new();

    private string _waferSerialNo = "";

    public string WaferSerialNo
    {
        get => _waferSerialNo;
        set
        {
            SetProperty(ref _waferSerialNo, value);
            AddCommand.RaiseCanExecuteChanged();
        }
    }


    private int _selectedSupplierId;

    public int SelectedSupplierId
    {
        get => _selectedSupplierId;
        set
        {
            SetProperty(ref _selectedSupplierId, value);
            AddCommand.RaiseCanExecuteChanged();
        }
    }

    private bool _showUnallocatedOnly;

    public bool ShowUnallocatedOnly
    {
        get => _showUnallocatedOnly;
        set
        {
            SetProperty(ref _showUnallocatedOnly, value);
            _ = LoadWafersAsync();
        }
    }

    private WaferMaster? _selectedWafer;

    public WaferMaster? SelectedWafer
    {
        get => _selectedWafer;
        set
        {
            SetProperty(ref _selectedWafer, value);
            DeleteCommand.RaiseCanExecuteChanged();
        }
    }

    public WaferViewModel(WaferService waferService,
        SupplierService supplierService)
    {
        _waferService = waferService;
        _supplierService = supplierService;


        AddCommand = new AsyncRelayCommand(
            execute: AddWaferAsync,
            canExecute: () =>
                !string.IsNullOrWhiteSpace(WaferSerialNo)
                && SelectedSupplierId > 0
        );

        DeleteCommand = new AsyncRelayCommand(
            execute: DeleteWaferAsync,
            canExecute: () => SelectedWafer != null
        );

        _ = LoadSuppliersAsync();
        _ = LoadWafersAsync();

    }


    public AsyncRelayCommand AddCommand { get; }

    public AsyncRelayCommand DeleteCommand { get; }


    // LOAD SUPPLIERS

    private async Task LoadSuppliersAsync()
    {
        var list = await _supplierService.GetAllAsync();

        Suppliers.Clear();

        foreach (var supplier in list)
        {
            Suppliers.Add(supplier);
        }
    }

    // LOAD WAFERS

    private async Task LoadWafersAsync()
    {
        IEnumerable<WaferMaster> list;

        if (ShowUnallocatedOnly)
        {
            list = await _waferService.GetUnallocatedAsync();
        }
        else
        {
            list = await _waferService.GetAllAsync();
        }

        Wafers.Clear();

        foreach (var wafer in list)
        {
            Wafers.Add(wafer);
        }
    }

    private async Task AddWaferAsync()
    {
        var wafer = new WaferMaster
        {
            WaferSerialNo = WaferSerialNo,
            SupplierId = SelectedSupplierId
        };

        var result = await _waferService.AddWaferAsync(wafer);

        MessageBox.Show(result.Message);

        if (result.Success)
        {
            await LoadWafersAsync();

            WaferSerialNo = "";
            SelectedSupplierId = 0;
        }
    }

    private async Task DeleteWaferAsync()
    {
        if (SelectedWafer == null)
            return;

        var result = await _waferService
            .DeleteWaferAsync(SelectedWafer.WaferId);

        MessageBox.Show(result.Message);

        if (result.Success)
        {
            Wafers.Remove(SelectedWafer);
        }
    }

}

