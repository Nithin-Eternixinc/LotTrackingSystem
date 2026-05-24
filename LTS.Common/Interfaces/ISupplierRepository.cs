using LTS.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTS.Common.Interfaces;

public interface ISupplierRepository
{
    Task<IEnumerable<Supplier>> GetAllAsync();
    Task<bool> ExistsByNameAsync(string name); //duplicte checking
    Task AddAsync(Supplier supplier);
    Task<bool> HasWafersAsync(int supplierId);
    Task UpdateAsync(Supplier supplier);
    Task DeleteAsync(int supplierId);
}
