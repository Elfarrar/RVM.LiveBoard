using RVM.LiveBoard.Domain.Entities;
using RVM.LiveBoard.Domain.Enums;

namespace RVM.LiveBoard.Domain.Interfaces;

public interface IAlertRepository
{
    Task<Alert?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Alert>> GetByStatusAsync(AlertStatus? status, int offset, int limit, CancellationToken ct = default);
    Task<List<Alert>> GetByRuleIdAsync(Guid ruleId, CancellationToken ct = default);
    Task AddAsync(Alert alert, CancellationToken ct = default);
    Task UpdateAsync(Alert alert, CancellationToken ct = default);
    Task<int> CountByStatusAsync(AlertStatus? status, CancellationToken ct = default);
}
