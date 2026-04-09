using RVM.LiveBoard.Domain.Entities;

namespace RVM.LiveBoard.Domain.Interfaces;

public interface IAlertRuleRepository
{
    Task<AlertRule?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<AlertRule>> GetAllAsync(CancellationToken ct = default);
    Task<List<AlertRule>> GetEnabledAsync(CancellationToken ct = default);
    Task AddAsync(AlertRule rule, CancellationToken ct = default);
    Task UpdateAsync(AlertRule rule, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
