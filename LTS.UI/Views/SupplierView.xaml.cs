using System;
using System.Text;
using System.Windows;
using LTS.UI.ViewModels;
using System.Windows.Controls;


namespace LTS.UI.Views
{
    /// <summary>
    /// Interaction logic for SupplierView.xaml
    /// </summary>
    public partial class SupplierView : UserControl
    {
        public SupplierView(SupplierViewModel vm)
        {
            InitializeComponent(); //load ui
            DataContext = vm; //All Bindings inside XAML should use this ViewModel.
        }

       
    }
}
