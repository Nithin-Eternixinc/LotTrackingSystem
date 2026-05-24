using System;
using System.Collections.Generic;
using System.Text;
using LTS.Application.Data;
using LTS.Common.Interfaces;
using LTS.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace LTS.Application.Repositories;

public class WaferRepository : IWaferRepository
{
    private readonly AppDbContext _db;

    public WaferRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<WaferMaster>> GetAllAsync()
    {
        return await _db.WaferMasters
            .Include(w => w.Supplier)
            .ToListAsync();
    }

    public async Task<WaferMaster?> GetByIdAsync(int waferId)
    {
        return await _db.WaferMasters
            .Include(w => w.Supplier)
            .FirstOrDefaultAsync(w => w.WaferId == waferId);

    }

    public async Task<IEnumerable<WaferMaster>> GetUnallocatedAsync()
    {
        return await _db.WaferMasters
            .Include(w => w.Supplier)
            .Where(w => w.WaferStatus == "Unallocated")
            .ToListAsync();
    }

    public async Task<int> GetUnallocatedCountAsync()
    {
        return await _db.WaferMasters
            .CountAsync(w => w.WaferStatus == "Unallocated");
    }

    public async Task<bool> ExistsBySerialAsync(string serialNo)
    {
        return await _db.WaferMasters
            .AnyAsync(w => w.WaferSerialNo == serialNo);
    }

    public async Task AddAsync(WaferMaster wafer)
    {
        _db.WaferMasters.Add(wafer);
        await _db.SaveChangesAsync();
    }

   
    public async Task DeleteAsync(int waferId)
    {
        var wafer = await _db.WaferMasters.FindAsync(waferId);
        if (wafer != null)
        {
            _db.WaferMasters.Remove(wafer);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> IsAllocatedAsync(int waferId) //check before deleting wafer if it allocated 
    {
        return await _db.WaferMasters
            .AnyAsync(w => w.WaferId == waferId &&
                           w.WaferStatus == "Allocated");
    }

    public async Task UpdateAsync(WaferMaster wafer)
    {
        _db.WaferMasters.Update(wafer);
        await _db.SaveChangesAsync();
    }

}
