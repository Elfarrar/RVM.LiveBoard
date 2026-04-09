using Microsoft.EntityFrameworkCore;
using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Interfaces;
using RVM.LiveBoard.Infrastructure.Data;

namespace RVM.LiveBoard.Infrastructure.Repositories;

public class AlertRuleRepository(LiveBoardDbContext db) : IAlertRuleRepository
{
    public Task<AlertRule?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.AlertRules.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<List<AlertRule>> GetAllAsync(CancellationToken ct = default)
        => db.AlertRules.OrderBy(r => r.Name).ToListAsync(ct);

    public Task<List<AlertRule>> GetEnabledAsync(CancellationToken ct = default)
        => db.AlertRules.Where(r => r.IsEnabled).ToListAsync(ct);

    public async Task AddAsync(AlertRule rule, CancellationToken ct = default)
    {
        db.AlertRules.Add(rule);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(AlertRule rule, CancellationToken ct = default)
    {
        db.AlertRules.Update(rule);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var rule = await db.AlertRules.FindAsync([id], ct);
        if (rule is not null)
        {
            db.AlertRules.Remove(rule);
            await db.SaveChangesAsync(ct);
        }
    }
}
