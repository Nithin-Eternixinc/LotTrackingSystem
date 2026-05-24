using System;
using System.Collections.Generic;
using System.Text;
using LTS.Application.Data;
using LTS.Common.Interfaces;
using LTS.Common.Models;
using Microsoft.EntityFrameworkCore;


namespace LTS.Application.Repositories;

public class LotRepository : ILotRepository

{
    private readonly AppDbContext _db;

    public LotRepository(AppDbContext db)
    {
        _db = db;
    }

    public  async Task<IEnumerable<Lot>> GetAllAsync()
    {
        return await _db.Lots
           .Include(l => l.Carrier)
           .OrderByDescending(l => l.CreatedOn)
           .Include(l => l.CurrentProcessLocation)
           .ToListAsync();
    }
    public async Task<Lot?> GetByIdAsync(int lotId)
    {
        return await _db.Lots
            .Include(l => l.Carrier)
            .FirstOrDefaultAsync(l => l.LotId == lotId);
    }

    public async Task AddAsync(Lot lot)
    {
        _db.Lots.Add(lot);
        await _db.SaveChangesAsync();
    }

   public async Task UpdateAsync(Lot lot)
    {
        _db.Lots.Update(lot);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int lotId)
    {
        var lot = await _db.Lots.FindAsync(lotId);
        if (lot!=null)
        {
            _db.Lots.Remove(lot);
            await _db.SaveChangesAsync();
        }
    }

}


