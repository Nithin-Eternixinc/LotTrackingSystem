using System;
using System.Collections.Generic;
using System.Text;
using LTS.Common.Models;

namespace LTS.UI.ViewModels;

public class SelectableWafer : ViewModelBase
{
    public WaferMaster Wafer { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
  
    public SelectableWafer(WaferMaster wafer)
    {
        Wafer = wafer;
    }
}
