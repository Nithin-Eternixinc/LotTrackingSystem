using LTS.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace LTS.UI.ViewModels;

public class LocationPickerItem : ViewModelBase  //represent one row in your Route Picker UI.
{
    private bool _isSelected;

    public int ProcessLocationId { get; set; }
    public string LocationName { get; set; } = "";
    public string Status { get; set; } = "";

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
public class RoutePickerViewModel : ViewModelBase
{
    public ObservableCollection<LocationPickerItem> LocationItems { get; set; } = new();

    public string SelectedCountText
        => $"{LocationItems.Count(l => l.IsSelected)} location(s) selected";

    // returns selected locations in the order they appear in the list
    public List<int> GetSelectedLocationIds()
        => LocationItems
            .Where(l => l.IsSelected)
            .Select(l => l.ProcessLocationId)
            .ToList();

    public RelayCommand MoveUpCommand { get; }
    public RelayCommand MoveDownCommand { get; }

    public RoutePickerViewModel(IEnumerable<ProcessLocation> locations)
    {
        foreach (var loc in locations)
        {

            var item = new LocationPickerItem
            {
                ProcessLocationId = loc.ProcessLocationId,
                LocationName = loc.Name,
                Status = loc.Status
            };
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LocationPickerItem.IsSelected))
                    OnPropertyChanged(nameof(SelectedCountText));
            };
            LocationItems.Add(item);

        }

        MoveUpCommand = new RelayCommand(obj =>
        {
            if (obj is not LocationPickerItem item) return;
            var idx = LocationItems.IndexOf(item);
            if (idx > 0)
                LocationItems.Move(idx, idx - 1);
        });

        MoveDownCommand = new RelayCommand(obj =>
        {
            if (obj is not LocationPickerItem item) return;
            var idx = LocationItems.IndexOf(item);
            if (idx < LocationItems.Count - 1)
                LocationItems.Move(idx, idx + 1);
        });
    }
}
