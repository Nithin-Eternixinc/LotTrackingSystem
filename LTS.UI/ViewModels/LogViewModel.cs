using LTS.Application.Services;
using LTS.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace LTS.UI.ViewModels;

public class LogViewModel : ViewModelBase
{
    private readonly LogService _logService;

    public ObservableCollection<ApplicationLog> Logs { get; } = new(); //smart list property that automatically updates the UI when you add/remove items.
    public LogViewModel(LogService logService)
    {
        _logService = logService;

        _ = LoadLogsAsync();
    }

    private async Task LoadLogsAsync()
    {
        var logs = await _logService.GetAllLogsAsync();

        Logs.Clear();

        foreach(var log in logs)
        {
            Logs.Add(log);
        }
    }
}
