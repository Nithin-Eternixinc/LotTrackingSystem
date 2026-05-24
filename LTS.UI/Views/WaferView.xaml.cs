using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using LTS.UI.ViewModels;

namespace LTS.UI.Views
{
    /// <summary>
    /// Interaction logic for WaferView.xaml
    /// </summary>
    public partial class WaferView : UserControl
    {
        public WaferView(WaferViewModel vm)  //
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
