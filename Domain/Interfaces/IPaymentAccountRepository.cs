using Domain.Entities;

namespace Domain.Interfaces;

public interface IPaymentAccountRepository
{
    Task<PaymentAccount?> GetByIdAsync(int id, CancellationToken ct);
    Task<List<PaymentAccount>> GetAllAsync(bool onlyActive, CancellationToken ct);
    Task AddAsync(PaymentAccount account, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
