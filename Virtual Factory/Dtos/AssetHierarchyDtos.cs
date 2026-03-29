namespace Virtual_Factory.Dtos
{
    public class AssetHierarchySiteDto
    {
        public string Site { get; set; } = string.Empty;
        public List<AssetHierarchyAreaDto> Areas { get; set; } = new();
    }

    public class AssetHierarchyAreaDto
    {
        public string Area { get; set; } = string.Empty;
        public List<AssetHierarchyLineDto> Lines { get; set; } = new();
    }

    public class AssetHierarchyLineDto
    {
        public string Line { get; set; } = string.Empty;
        public List<AssetHierarchyEquipmentDto> Equipment { get; set; } = new();
    }

    public class AssetHierarchyEquipmentDto
    {
        public string EquipmentName { get; set; } = string.Empty;
        public int TopicCount { get; set; }
    }
}