using Proxy.Models;

namespace Proxy.Adapters;

public interface IAdapter
{
    public Task<IAdapter> InitializeAsync();

    public Task<Crossing[]> GetCrossingStatesAsync(string[]? crossingIds = default);

    public Task<Crossing[]> CallCrossingsAsync(string[] crossingIds, bool extended = false);
    
}