using LTS.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LTS.UI.Views
{
    /// <summary>
    /// Interaction logic for LotView.xaml
    /// </summary>
    public partial class LotView : UserControl
    {
        public LotView(LotViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
