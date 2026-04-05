namespace Virtual_Factory.Dtos
{
    public sealed class AssistantAskRequestDto
    {
        public string?        Equipment        { get; set; }
        public string?        Issue            { get; set; }
        public double?        Availability1h   { get; set; }
        public int?           Stops24h         { get; set; }
        public int?           Alarms24h        { get; set; }
        public string?        Sku              { get; set; }
        public List<string>?  SignalExceptions { get; set; }
    }
}
