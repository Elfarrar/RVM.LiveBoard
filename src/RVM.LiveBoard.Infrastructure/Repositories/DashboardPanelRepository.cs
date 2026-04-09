using Microsoft.EntityFrameworkCore;
using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Interfaces;
using RVM.LiveBoard.Infrastructure.Data;

namespace RVM.LiveBoard.Infrastructure.Repositories;

public class DashboardPanelRepository(LiveBoardDbContext db) : IDashboardPanelRepository
{
    public Task<DashboardPanel?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.DashboardPanels.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<List<DashboardPanel>> GetByDashboardIdAsync(Guid dashboardId, CancellationToken ct = default)
        => db.DashboardPanels.Where(p => p.DashboardId == dashboardId)
            .OrderBy(p => p.SortOrder).ToListAsync(ct);

    public async Task AddAsync(DashboardPanel panel, CancellationToken ct = default)
    {
        db.DashboardPanels.Add(panel);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(DashboardPanel panel, CancellationToken ct = default)
    {
        db.DashboardPanels.Update(panel);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var panel = await db.DashboardPanels.FindAsync([id], ct);
        if (panel is not null)
        {
            db.DashboardPanels.Remove(panel);
            await db.SaveChangesAsync(ct);
        }
    }
}
