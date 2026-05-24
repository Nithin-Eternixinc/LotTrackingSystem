using System;
using System.Collections.Generic;
using System.Text;
using LTS.Application.Services;
using LTS.Common.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;


namespace LTS.UI.ViewModels;

public class CarrierViewModel : ViewModelBase
{
    private readonly CarrierService _carrierService;

    public ObservableCollection<Carrier> Carriers { get; set; } = new();

    //carrriercode

    private string _carrierCode = "";
    public string CarrierCode
    {
        get => _carrierCode;
        set
        {
            SetProperty(ref _carrierCode, value);
            AddCommand.RaiseCanExecuteChanged();
        }
    }

    // --- Capacity ---

    private int _capacity = 25;
    public int Capacity
    {
        get => _capacity;
        set
        {
            SetProperty(ref _capacity, value > 25 ? 25 : value);
            AddCommand.RaiseCanExecuteChanged();
        }
    }

    private Carrier? _selectedCarrier; //store currently selected Carrier object.
    public Carrier? SelectedCarrier
    {
        get => _selectedCarrier;
        set
        {
            SetProperty(ref _selectedCarrier, value);
            DeleteCommand.RaiseCanExecuteChanged();
        }
    }


    public AsyncRelayCommand AddCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }

    public CarrierViewModel(CarrierService carrierService)
    {
        _carrierService = carrierService;

        AddCommand = new AsyncRelayCommand(
            execute: AddCarrierAsync,
            canExecute: () =>
            !string.IsNullOrWhiteSpace(CarrierCode)
            && Capacity > 0
            && Capacity <= 25);

        DeleteCommand = new AsyncRelayCommand(
            execute: DeleteCarrierAsync,
            canExecute: () => SelectedCarrier != null
        );

        _ = LoadCarriersAsync();

    }

    //load

    private async Task LoadCarriersAsync()
    {
        var list = await _carrierService.GetAllCarriersAsync();

        Carriers.Clear();

        foreach (var carrier in list)
        {
            Carriers.Add(carrier);

        }

    }

    //add

    private async Task AddCarrierAsync()
    {
        var result = await _carrierService.AddCarrierAsync(CarrierCode, Capacity);

        MessageBox.Show(result.Message);

        if (result.Success)
        {
            await LoadCarriersAsync();
            CarrierCode = "";
            Capacity = 25;
        }
    }

    //delte
    private async Task DeleteCarrierAsync()
    {
        if (SelectedCarrier == null)
            return;

        if (SelectedCarrier.Status == "Allocated")
        {
            MessageBox.Show("Cannot delete an allocated carrier. It is currently in use by a lot.");
            return;
        }

        var result = await _carrierService.DeleteCarrierAsync(SelectedCarrier.CarrierId);

        MessageBox.Show(result.Message);

        if (result.Success)
        {
            Carriers.Remove(SelectedCarrier);
        }
    }


}