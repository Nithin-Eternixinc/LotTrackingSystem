using System;
using System.Collections.Generic;
using System.Text;
using LTS.Application.Data;
using LTS.Common.Interfaces;
using LTS.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace LTS.Application.Repositories;

public class ProcessLocationRepository : IProcessLocationRepository
{
    private readonly AppDbContext _db;
    public ProcessLocationRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<ProcessLocation>> GetAllAsync()
    {
        return await _db.ProcessLocations
            .Include(p => p.CurrentLot)
            .OrderBy(p => p.SequenceNo)
            .ToListAsync();
    }

    public async Task<ProcessLocation?> GetByIdAsync(int id)
    {
        return await _db.ProcessLocations
             .Include(p => p.CurrentLot)
             .FirstOrDefaultAsync(p => p.ProcessLocationId == id);
    }
    public async Task UpdateAsync(ProcessLocation location)
    {
        _db.ProcessLocations.Update(location);
        await _db.SaveChangesAsync();
    }
}