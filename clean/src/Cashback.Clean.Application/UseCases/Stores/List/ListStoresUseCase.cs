using Cashback.Clean.Application.Abstractions;
using Cashback.Clean.Application.Dtos.Responses;

namespace Cashback.Clean.Application.UseCases.Stores.List;

public interface IListStoresUseCase
{
    Task<IList<ResponseStoreJson>> Execute();
}

public class ListStoresUseCase(IStoreRepository storeRepository) : IListStoresUseCase
{
    public async Task<IList<ResponseStoreJson>> Execute() =>
        (await storeRepository.List()).Select(s => new ResponseStoreJson(s.Id, s.Name, s.Category)).ToList();
}
