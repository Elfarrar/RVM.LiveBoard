using Microsoft.EntityFrameworkCore;
using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Enums;
using RVM.LiveBoard.Domain.Interfaces;
using RVM.LiveBoard.Infrastructure.Data;

namespace RVM.LiveBoard.Infrastructure.Repositories;

public class AlertRepository(LiveBoardDbContext db) : IAlertRepository
{
    public Task<Alert?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Alerts.Include(a => a.Rule).FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<List<Alert>> GetByStatusAsync(AlertStatus? status, int offset, int limit, CancellationToken ct = default)
    {
        var q = db.Alerts.Include(a => a.Rule).AsQueryable();

        if (status.HasValue)
            q = q.Where(a => a.Status == status.Value);

        return await q.OrderByDescending(a => a.FiredAt)
            .Skip(offset).Take(limit)
            .ToListAsync(ct);
    }

    public Task<List<Alert>> GetByRuleIdAsync(Guid ruleId, CancellationToken ct = default)
        => db.Alerts.Where(a => a.AlertRuleId == ruleId)
            .OrderByDescending(a => a.FiredAt).ToListAsync(ct);

    public async Task AddAsync(Alert alert, CancellationToken ct = default)
    {
        db.Alerts.Add(alert);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Alert alert, CancellationToken ct = default)
    {
        db.Alerts.Update(alert);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> CountByStatusAsync(AlertStatus? status, CancellationToken ct = default)
    {
        var q = db.Alerts.AsQueryable();
        if (status.HasValue)
            q = q.Where(a => a.Status == status.Value);
        return await q.CountAsync(ct);
    }
}
