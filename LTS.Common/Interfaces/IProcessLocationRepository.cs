using LTS.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LTS.Common.Interfaces;

public interface IProcessLocationRepository
{
    Task<IEnumerable<ProcessLocation>> GetAllAsync();
    Task<ProcessLocation?> GetByIdAsync(int id);
    Task UpdateAsync(ProcessLocation location);
}
