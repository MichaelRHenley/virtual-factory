using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public sealed class AssistantService : IAssistantService
    {
        private readonly HttpClient                         _http;
        private readonly IEquipmentAssistantContextBuilder  _contextBuilder;
        private readonly ILogger<AssistantService>          _log;
        private readonly string                             _model;

        public AssistantService(
            HttpClient http,
            IEquipmentAssistantContextBuilder contextBuilder,
            ILogger<AssistantService> log,
            IConfiguration configuration)
        {
            _http           = http;
            _contextBuilder = contextBuilder;
            _log            = log;
            _model          = configuration["Ollama:Model"] ?? "llama3";
        }

        public async Task<AssistantResponseDto?> GetEquipmentAssistantResponseAsync(
            string equipmentId,
            CancellationToken cancellationToken = default)
        {
            _log.LogInformation(
                "AssistantService.GetEquipmentAssistantResponseAsync: calling BuildAsync for {EquipmentId}",
                equipmentId);

            var context = await _contextBuilder.BuildAsync(equipmentId, cancellationToken);

            _log.LogInformation(
                "AssistantService: BuildAsync returned {ContextStatus} for {EquipmentId}, summaryLength={Length}",
                context is null ? "null" : "non-null",
                equipmentId,
                context?.HumanSummary.Length ?? -1);

            if (context is null)
            {
                return new AssistantResponseDto
                {
                    EquipmentId       = equipmentId,
                    ContextSummary    = string.Empty,
                    AssistantResponse = "No operational context available for this equipment.",
                };
            }

            _log.LogInformation("Assistant context built for {EquipmentId}", equipmentId);

            var prompt =
                $"""
                You are a manufacturing operations assistant.

                The assistant must ONLY draw conclusions from the provided context data
                (telemetry signals, signal health classifications, signal metadata
                interpretation hints, production orders, BOM and inventory status,
                preventive maintenance tasks, and equipment event history).
                Do NOT speculate about causes that are not explicitly supported by
                this context.

                If there is no evidence for a specific cause, you MUST say:
                "No supporting evidence available in current telemetry context"
                instead of suggesting a speculative cause.

                Structure your markdown response using the following headings:
                1. Current Condition
                2. Signal Health
                3. Operational Context
                4. Recent Activity
                5. Maintenance Status
                6. Risk Assessment
                7. Suggested Checks

                Rules for Suggested Causes / Checks:
                - Only mention causes or risks when supported by at least one of:
                  * alarm history
                  * recent stop events
                  * overdue preventive maintenance tasks
                  * signals deviating from their normal band
                  * material shortage or blocked BOM inputs
                  * mismatch between active production order and schedule
                  * significant degradation in availability metrics
                - If none of the above evidence types are present, omit any
                  "Suggested causes" narrative and keep Suggested Checks minimal
                  or state that no specific causes can be identified.
                - Do NOT use generic guesses like "operator error", "sensor fault",
                  or "mechanical issue" unless they are directly referenced by the
                  context data.
                - For signal-related insights, rely ONLY on the provided
                  signal classifications (Normal, NearLimit, High, Low) and any
                  associated interpretation hints. If no hint exists, you must
                  not invent a meaning for that signal.
                - When providing Suggested Checks, always tie each check to concrete
                  evidence from the context (for example, an overdue PM task, a high
                  vibration signal, a recent stop event, or a material shortage).

                Below is the structured context you should rely on. Treat it as the
                complete set of facts – do not assume anything that is not stated.

                ## Input Summary
                {context.InputSummary}

                ## Context Summary
                {context.ContextSummary}

                Now respond in markdown using the required headings and evidence-based rules.
                """;

            var requestBody = new OllamaGenerateRequest(_model, prompt, Stream: false);

            _log.LogInformation("Calling Ollama with structured context for {EquipmentId}", equipmentId);

            HttpResponseMessage httpResponse;
            try
            {
                httpResponse = await _http.PostAsJsonAsync("api/generate", requestBody, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogWarning(ex, "Ollama unreachable for {EquipmentId} — returning context summary as response", equipmentId);
                return new AssistantResponseDto
                {
                    EquipmentId       = equipmentId,
                    InputSummary      = context.HumanSummary,
                    ContextSummary    = context.HumanSummary,
                    AssistantResponse = context.HumanSummary,
                };
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                var detail = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                return new AssistantResponseDto
                {
                    EquipmentId       = equipmentId,
                    InputSummary      = context.HumanSummary,
                    ContextSummary    = context.HumanSummary,
                    AssistantResponse =
                        $"Ollama returned {(int)httpResponse.StatusCode} " +
                        $"{httpResponse.StatusCode}. The model '{_model}' may not be " +
                        $"installed. Run 'ollama pull {_model}' to install it. " +
                        $"Detail: {detail.Trim()}",
                };
            }

            var ollamaResponse = await httpResponse.Content
                .ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken);

            return new AssistantResponseDto
            {
                EquipmentId       = equipmentId,
                InputSummary      = context.InputSummary,
                ContextSummary    = context.ContextSummary,
                AssistantResponse = ollamaResponse?.Response ?? string.Empty,
            };
        }

        public Task<AssistantResponseDto?> AskAsync(
            string? equipmentName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(equipmentName))
                return Task.FromResult<AssistantResponseDto?>(null);

            return GetEquipmentAssistantResponseAsync(equipmentName, cancellationToken);
        }

        // ── Ollama wire types ──────────────────────────────────────────────────────

        private sealed record OllamaGenerateRequest(
            string Model,
            string Prompt,
            [property: JsonPropertyName("stream")] bool Stream);

        private sealed record OllamaGenerateResponse(
            [property: JsonPropertyName("response")] string Response);
    }
}
