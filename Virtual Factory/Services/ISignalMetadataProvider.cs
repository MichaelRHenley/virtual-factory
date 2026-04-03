using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public interface ISignalMetadataProvider
    {
        SignalMetadataDto? GetMetadata(string signalName);
    }
}
