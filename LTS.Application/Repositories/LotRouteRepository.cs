using System;
using System.Collections.Generic;
using System.Text;
using LTS.Application.Data;
using LTS.Common.Interfaces;
using LTS.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace LTS.Application.Repositories;

public class LotRouteRepository : ILotRouteRepository
{
    private readonly AppDbContext _db;
    public LotRouteRepository(AppDbContext db) => _db = db;

    public async Task<IEnumerable<LotRoute>> GetByLotIdAsync(int lotId)
     => await _db.LotRoutes
         .Include(r => r.ProcessLocation)
         .Where(r => r.LotId == lotId)
         .OrderBy(r => r.StepOrder)
         .ToListAsync();

    public async Task AddAsync(LotRoute route)
    {
        _db.LotRoutes.Add(route);
        await _db.SaveChangesAsync();
    }
    public async Task UpdateAsync(LotRoute route)
    {
        _db.LotRoutes.Update(route);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteByLotIdAsync(int lotId)
    {
        var routes = await _db.LotRoutes.Where(r => r.LotId == lotId).ToListAsync();
        _db.LotRoutes.RemoveRange(routes);
        await _db.SaveChangesAsync();
    }

}
