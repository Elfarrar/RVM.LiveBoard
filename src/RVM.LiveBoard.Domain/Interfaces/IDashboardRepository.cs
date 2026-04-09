using RVM.LiveBoard.Domain.Entities;

namespace RVM.LiveBoard.Domain.Interfaces;

public interface IDashboardRepository
{
    Task<Dashboard?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Dashboard?> GetByIdWithPanelsAsync(Guid id, CancellationToken ct = default);
    Task<List<Dashboard>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Dashboard dashboard, CancellationToken ct = default);
    Task UpdateAsync(Dashboard dashboard, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
