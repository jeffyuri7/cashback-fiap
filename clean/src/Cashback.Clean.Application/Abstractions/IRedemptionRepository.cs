using Cashback.Clean.Domain.Entities;

namespace Cashback.Clean.Application.Abstractions;

public interface IRedemptionRepository
{
    Task Add(Redemption redemption);
}
