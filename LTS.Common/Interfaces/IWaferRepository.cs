using LTS.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTS.Common.Interfaces;

public interface IWaferRepository
{
    Task<IEnumerable<WaferMaster>> GetAllAsync();
    Task<WaferMaster?> GetByIdAsync(int waferId);
    Task<IEnumerable<WaferMaster>> GetUnallocatedAsync(); //lot creation needs only FREE wafers shown
    Task<int> GetUnallocatedCountAsync();
    Task<bool> ExistsBySerialAsync(string serialNo);
    Task AddAsync(WaferMaster wafer);
    Task DeleteAsync(int waferId);
    Task<bool> IsAllocatedAsync(int waferId);
    Task UpdateAsync(WaferMaster wafer);
}