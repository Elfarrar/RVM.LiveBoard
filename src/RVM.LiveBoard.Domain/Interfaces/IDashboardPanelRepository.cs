using RVM.LiveBoard.Domain.Entities;

namespace RVM.LiveBoard.Domain.Interfaces;

public interface IDashboardPanelRepository
{
    Task<DashboardPanel?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<DashboardPanel>> GetByDashboardIdAsync(Guid dashboardId, CancellationToken ct = default);
    Task AddAsync(DashboardPanel panel, CancellationToken ct = default);
    Task UpdateAsync(DashboardPanel panel, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
