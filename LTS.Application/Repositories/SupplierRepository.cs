using System;
using System.Collections.Generic;
using System.Text;
using LTS.Application.Data;
using LTS.Common.Interfaces;
using LTS.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace LTS.Application.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly AppDbContext _db;

    public SupplierRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Supplier>> GetAllAsync()
    {
        return await _db.Suppliers
            .Where(s => s.IsActive)
            .ToListAsync();
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _db.Suppliers
            .AnyAsync(s => s.SupplierName.ToLower() == name.ToLower());
    }

    public async Task AddAsync(Supplier Supplier)
    {
        _db.Suppliers.Add(Supplier);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> HasWafersAsync(int supplierId)
    {
        return await _db.WaferMasters
            .AnyAsync(w => w.SupplierId == supplierId);
    }

    public async Task UpdateAsync(Supplier Supplier)
    {
        _db.Suppliers.Update(Supplier);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int supplierId)
    {
        var supplier = await _db.Suppliers.FindAsync(supplierId);

        if(supplier != null)
        {
            supplier.IsActive = false;
            await _db.SaveChangesAsync();
        }
    }

}
