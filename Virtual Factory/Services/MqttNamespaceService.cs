using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using Virtual_Factory.Models;

namespace Virtual_Factory.Services
{
    /// <summary>
    /// Hosted service that connects to a local MQTT broker, subscribes to namespace topics,
    /// and mirrors incoming MQTT messages into the latest value store.
    /// </summary>
    public sealed class MqttNamespaceService : BackgroundService
    {
        private readonly ILatestPointValueStore _store;
        private readonly ILogger<MqttNamespaceService> _logger;
        private IMqttClient? _client;

        public MqttNamespaceService(
            ILatestPointValueStore store,
            ILogger<MqttNamespaceService> logger)
        {
            _store = store;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new MqttClientFactory();
            _client = factory.CreateMqttClient();

            _client.ConnectedAsync += e =>
            {
                _logger.LogInformation("MQTT connected to broker.");
                return Task.CompletedTask;
            };

            _client.DisconnectedAsync += async e =>
            {
                _logger.LogWarning("MQTT disconnected: {Reason}", e.ReasonString);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                    await TryConnectAsync(_client, factory, stoppingToken);
                }
            };

            _client.ApplicationMessageReceivedAsync += e =>
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = e.ApplicationMessage.ConvertPayloadToString() ?? string.Empty;

                _store.SetValue(new LatestPointValue
                {
                    Topic = topic,
                    Value = payload,
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Status = "Good",
                    Source = "mqtt"
                });

                _logger.LogInformation("MQTT received: {Topic} = {Payload}", topic, payload);
                return Task.CompletedTask;
            };

            await TryConnectAsync(_client, factory, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task TryConnectAsync(
            IMqttClient client,
            MqttClientFactory factory,
            CancellationToken stoppingToken)
        {
            var options = new MqttClientOptionsBuilder()
                .WithClientId("virtual-factory-simulator")
                .WithTcpServer("localhost", 1883)
                .Build();

            while (!stoppingToken.IsCancellationRequested && !client.IsConnected)
            {
                try
                {
                    await client.ConnectAsync(options, stoppingToken);

                    var subscribeOptions = factory.CreateSubscribeOptionsBuilder()
                        .WithTopicFilter(f => f.WithTopic("enterprise/#").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
                        .WithTopicFilter(f => f.WithTopic("virtual-enterprise/#").WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
                        .Build();

                    await client.SubscribeAsync(subscribeOptions, stoppingToken);

                    _logger.LogInformation("MQTT subscribed to enterprise/# and virtual-enterprise/#");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "MQTT connection failed. Retrying in 3 seconds.");
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
            }
        }
    }
}