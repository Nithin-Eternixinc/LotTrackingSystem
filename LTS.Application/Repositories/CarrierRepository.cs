using System;
using System.Collections.Generic;
using System.Text;
using LTS.Application.Data;
using LTS.Common.Interfaces;
using LTS.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace LTS.Application.Repositories;

public class CarrierRepository : ICarrierRepository

{
    private readonly AppDbContext _db; //field to store db context on cunstructr.

    public CarrierRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Carrier>> GetAllAsync()
    {
        return await _db.Carriers
            .OrderByDescending(c => c.CreatedOn)
            .ToListAsync();
    }
    public async Task<Carrier?> GetByIdAsync(int carrierId)
    {
        return await _db.Carriers.FindAsync(carrierId);
    }
    public async Task AddAsync(Carrier carrier)
    {
        _db.Carriers.Add(carrier);
        await _db.SaveChangesAsync();

    }

    public async Task DeleteAsync(int carrierId)
    {
        var carrier = await _db.Carriers.FindAsync(carrierId);
        if (carrier != null)
        {
            _db.Carriers.Remove(carrier);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsByCodeAsync(string code)
    {
        return await _db.Carriers
            .AnyAsync(c => c.CarrierCode == code);
    }

    public async Task<IEnumerable<Carrier>> GetAvailableAsync()
    {
        return await _db.Carriers
            .Where(c => c.Status == "Unallocated")
            .ToListAsync();
    }

    public async Task UpdateAsync(Carrier carrier)
    {
        _db.Carriers.Update(carrier);
        await _db.SaveChangesAsync();
    }

}


