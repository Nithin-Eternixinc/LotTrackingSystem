using System;
using System.Collections.Generic;
using System.Text;
using LTS.Application.Services;
using LTS.Common.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Drawing.Printing;

namespace LTS.UI.ViewModels

{
    public class SupplierViewModel : ViewModelBase
    {
        private readonly SupplierService _supplierService;

        public ObservableCollection<Supplier> Suppliers { get; set; } = new(); //list

        private string _supplierName = "";
        public string SupplierName
        {
            get => _supplierName;
            set
            {
                SetProperty(ref _supplierName, value);
                AddCommand.RaiseCanExecuteChanged(); //Rechecks whether Add button should be enabled.
            }
        }

        private string _contactName = "";
        public string ContactName
        {
            get => _contactName;
            set => SetProperty(ref _contactName, value);
        }

        private string _contactEmail = "";
        public string ContactEmail
        {
            get => _contactEmail;
            set => SetProperty(ref _contactEmail, value);
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private Supplier? _selectedSupplier; //Stores currently selected DataGrid row.
        public Supplier? SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                SetProperty(ref _selectedSupplier, value);
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        // Commands

        public AsyncRelayCommand AddCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }

        public SupplierViewModel(SupplierService supplierService)
        {
            _supplierService = supplierService;

            AddCommand = new AsyncRelayCommand(
              execute: AddSupplierAsync,
              canExecute: () =>
                  !string.IsNullOrWhiteSpace(SupplierName)
          );


            DeleteCommand = new AsyncRelayCommand(
                execute: DeleteSupplierAsync,
                canExecute: () =>
                    SelectedSupplier != null
            );

            _ = LoadSuppliersAsync();

        }


        // Load Suppliers


        private async Task LoadSuppliersAsync()
        {
            var list = await _supplierService.GetAllAsync();

            Suppliers.Clear();

            foreach (var supplier in list)
            {
                Suppliers.Add(supplier);
            }
        }

        // Add Suppliers

        private async Task AddSupplierAsync()
        {
            ErrorMessage = "";

            var supplier = new Supplier
            {
                SupplierName = SupplierName,
                ContactName = ContactName,
                ContactEmail = ContactEmail
            };

            var result = await _supplierService.AddSupplierAsync(supplier);

            if (result.Success)
            {
                await LoadSuppliersAsync();

                // Clear form
                SupplierName = "";
                ContactName = "";
                ContactEmail = "";
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }

        // Delete Supplier

        private async Task DeleteSupplierAsync()
        {
            if (SelectedSupplier == null)
                return;

            var result = await _supplierService
                .DeleteSupplierAsync(SelectedSupplier.SupplierId);

            if (result.Success)
            {
                Suppliers.Remove(SelectedSupplier);
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }

    }
}
