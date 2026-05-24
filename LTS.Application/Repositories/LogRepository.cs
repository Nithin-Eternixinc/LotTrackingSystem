using LTS.Application.Data;
using LTS.Common.Interfaces;
using LTS.Common.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;


namespace LTS.Application.Repositories;

public class LogRepository : ILogRepository
{
    private readonly AppDbContext _db; //holds a database context

    public LogRepository(AppDbContext db)  //constructor takes AppDbContext as a parameter and saves it in _db
    {
        _db = db;
    }
    public async Task AddAsync(ApplicationLog log)
    {
        _db.ApplicationLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<ApplicationLog>> GetAllAsync()
    {
        return await _db.ApplicationLogs
             .AsNoTracking()
            .OrderByDescending(l => l.Timestamp) // newest first
            .ToListAsync();
    }
}
