using LTS.Common.Interfaces;
using LTS.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTS.Application.Services;
public class LogService

{
    private readonly ILogRepository _logRepo;

    public LogService(ILogRepository logRepo)
    {
        _logRepo = logRepo;
    }

    public async Task LogInfoAsync(string message)
    {
        await _logRepo.AddAsync(new ApplicationLog 
        {
            Level = "INFO",
            Message = message,
            Timestamp =DateTime.Now
        });
    }

    public async Task LogWarnAsync(string message)
    {
        await _logRepo.AddAsync(new ApplicationLog
        {
            Level = "WARN",
            Message = message,
            Timestamp = DateTime.Now
        });
    }

    public async Task LogErrorAsync(string message)
    {
        await _logRepo.AddAsync(new ApplicationLog
        {
            Level = "ERROR",
            Message = message,
            Timestamp = DateTime.Now
        });
    }

    public async Task<IEnumerable<ApplicationLog>> GetAllLogsAsync()
    {
        return await _logRepo.GetAllAsync();
    }

}
