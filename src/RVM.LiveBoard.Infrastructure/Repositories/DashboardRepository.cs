using Microsoft.EntityFrameworkCore;
using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Interfaces;
using RVM.LiveBoard.Infrastructure.Data;

namespace RVM.LiveBoard.Infrastructure.Repositories;

public class DashboardRepository(LiveBoardDbContext db) : IDashboardRepository
{
    public Task<Dashboard?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Dashboards.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<Dashboard?> GetByIdWithPanelsAsync(Guid id, CancellationToken ct = default)
        => db.Dashboards.Include(d => d.Panels.OrderBy(p => p.SortOrder))
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<List<Dashboard>> GetAllAsync(CancellationToken ct = default)
        => db.Dashboards.OrderBy(d => d.Name).ToListAsync(ct);

    public async Task AddAsync(Dashboard dashboard, CancellationToken ct = default)
    {
        db.Dashboards.Add(dashboard);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Dashboard dashboard, CancellationToken ct = default)
    {
        db.Dashboards.Update(dashboard);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var dashboard = await db.Dashboards.FindAsync([id], ct);
        if (dashboard is not null)
        {
            db.Dashboards.Remove(dashboard);
            await db.SaveChangesAsync(ct);
        }
    }
}
