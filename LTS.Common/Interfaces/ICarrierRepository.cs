using System;
using System.Collections.Generic;
using System.Text;

using LTS.Common.Models;

namespace LTS.Common.Interfaces;

public interface ICarrierRepository
{
    Task<IEnumerable<Carrier>> GetAllAsync();
    Task<Carrier?> GetByIdAsync(int carrierId);
    Task AddAsync(Carrier carrier);
    Task<bool> ExistsByCodeAsync(string code);
    Task<IEnumerable<Carrier>> GetAvailableAsync();
    Task DeleteAsync(int carrierId);
    Task UpdateAsync(Carrier carrier);
}