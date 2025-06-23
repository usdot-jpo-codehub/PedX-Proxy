// filepath: c:\Users\C0011022\Projects\PedX\src\Proxy\Services\IAdapterFactory.cs
using Proxy.Adapters;
using Proxy.Configs;
using Proxy.Models;

namespace Proxy.Services;

public interface IAdapterFactory
{
    Task<IAdapter> GetAdapterAsync(string intersectionId);
    Task<IAdapter> GetAdapterAsync(IntersectionConfig intersectionConfig);
    IEnumerable<Intersection> GetIntersections();
    Intersection GetIntersection(string intersectionId);
}
