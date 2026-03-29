using Virtual_Factory.Dtos;
using Virtual_Factory.Infrastructure;
using Virtual_Factory.Models;

namespace Virtual_Factory.Services
{
    public class AssetHierarchyService : IAssetHierarchyService
    {
        public List<AssetHierarchySiteDto> BuildHierarchy(IEnumerable<LatestPointValue> points)
        {
            var equipmentTopicCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var map = new Dictionary<string, Dictionary<string, Dictionary<string, HashSet<string>>>>(StringComparer.OrdinalIgnoreCase);

            foreach (var point in points)
            {
                if (string.IsNullOrWhiteSpace(point.Topic))
                    continue;

                var parsed = TopicParser.TryParse(point.Topic);
                if (parsed is null)
                    continue;

                var (site, area, line, equipment) = parsed.Value;

                if (!map.TryGetValue(site, out var areas))
                {
                    areas = new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);
                    map[site] = areas;
                }

                if (!areas.TryGetValue(area, out var lines))
                {
                    lines = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                    areas[area] = lines;
                }

                if (!lines.TryGetValue(line, out var equipmentSet))
                {
                    equipmentSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    lines[line] = equipmentSet;
                }

                equipmentSet.Add(equipment);

                var equipmentKey = $"{site}|{area}|{line}|{equipment}";
                if (!equipmentTopicCounts.ContainsKey(equipmentKey))
                    equipmentTopicCounts[equipmentKey] = 0;

                equipmentTopicCounts[equipmentKey]++;
            }

            var result = new List<AssetHierarchySiteDto>();

            foreach (var sitePair in map.OrderBy(x => x.Key))
            {
                var siteDto = new AssetHierarchySiteDto
                {
                    Site = sitePair.Key
                };

                foreach (var areaPair in sitePair.Value.OrderBy(x => x.Key))
                {
                    var areaDto = new AssetHierarchyAreaDto
                    {
                        Area = areaPair.Key
                    };

                    foreach (var linePair in areaPair.Value.OrderBy(x => x.Key))
                    {
                        var lineDto = new AssetHierarchyLineDto
                        {
                            Line = linePair.Key
                        };

                        foreach (var equipmentName in linePair.Value.OrderBy(x => x))
                        {
                            var equipmentKey = $"{sitePair.Key}|{areaPair.Key}|{linePair.Key}|{equipmentName}";

                            lineDto.Equipment.Add(new AssetHierarchyEquipmentDto
                            {
                                EquipmentName = equipmentName,
                                TopicCount = equipmentTopicCounts.TryGetValue(equipmentKey, out var count) ? count : 0
                            });
                        }

                        areaDto.Lines.Add(lineDto);
                    }

                    siteDto.Areas.Add(areaDto);
                }

                result.Add(siteDto);
            }

            return result;
        }

    }
}