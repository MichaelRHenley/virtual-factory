using Virtual_Factory.Dtos;
using Virtual_Factory.Models;

namespace Virtual_Factory.Services
{
    public interface IAssetHierarchyService
    {
        List<AssetHierarchySiteDto> BuildHierarchy(IEnumerable<LatestPointValue> points);
    }
}