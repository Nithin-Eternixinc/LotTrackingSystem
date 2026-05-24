using LTS.Common.Models;

namespace LTS.Common.Interfaces;

public interface ILotRouteRepository
{
    Task<IEnumerable<LotRoute>> GetByLotIdAsync(int lotId);
    Task AddAsync(LotRoute route);
    Task UpdateAsync(LotRoute route);
    Task DeleteByLotIdAsync(int lotId);
}