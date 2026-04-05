using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public sealed class AssistantService : IAssistantService
    {
        private readonly HttpClient                         _http;
        private readonly IEquipmentAssistantContextBuilder  _contextBuilder;
        private readonly IEquipmentEventSummaryService      _eventSummary;
        private readonly ILogger<AssistantService>          _log;
        private readonly string                             _model;

        public AssistantService(
            HttpClient http,
            IEquipmentAssistantContextBuilder contextBuilder,
            IEquipmentEventSummaryService eventSummary,
            ILogger<AssistantService> log,
            IConfiguration configuration)
        {
            _http           = http;
            _contextBuilder = contextBuilder;
            _eventSummary   = eventSummary;
            _log            = log;
            _model          = configuration["Ollama:Model"] ?? "llama3";
        }

        public async Task<AssistantResponseDto?> GetEquipmentAssistantResponseAsync(
            string equipmentId,
            CancellationToken cancellationToken = default)
        {
            var overallStart = DateTime.UtcNow;
            _log.LogInformation(
                "AssistantService.GetEquipmentAssistantResponseAsync ENTER for {EquipmentId} at {StartTime}",
                equipmentId,
                overallStart);

            var ctxStart = DateTime.UtcNow;
            _log.LogInformation(
                "AssistantService context build START for {EquipmentId} at {StartTime}",
                equipmentId,
                ctxStart);

            var context = await _contextBuilder.BuildAsync(equipmentId, cancellationToken);

            var ctxEnd = DateTime.UtcNow;
            var ctxMs = (ctxEnd - ctxStart).TotalMilliseconds;

            _log.LogInformation(
                "AssistantService context build END for {EquipmentId} after {DurationMs} ms (status={ContextStatus}, summaryLength={Length})",
                equipmentId,
                ctxMs,
                context is null ? "null" : "non-null",
                context?.HumanSummary.Length ?? -1);

            if (context is null)
            {
                var overallEndNoCtx = DateTime.UtcNow;
                var overallMsNoCtx = (overallEndNoCtx - overallStart).TotalMilliseconds;
                _log.LogInformation(
                    "AssistantService EXIT for {EquipmentId} after {DurationMs} ms (no context)",
                    equipmentId,
                    overallMsNoCtx);

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

            var llmStart = DateTime.UtcNow;
            _log.LogInformation(
                "AssistantService LLM call START for {EquipmentId} at {StartTime}",
                equipmentId,
                llmStart);

            HttpResponseMessage httpResponse;
            try
            {
                httpResponse = await _http.PostAsJsonAsync("api/generate", requestBody, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var llmFailEnd = DateTime.UtcNow;
                var llmFailMs = (llmFailEnd - llmStart).TotalMilliseconds;
                _log.LogWarning(ex,
                    "AssistantService LLM call FAILED for {EquipmentId} after {DurationMs} ms — returning context summary as response",
                    equipmentId,
                    llmFailMs);

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
                var llmEndError = DateTime.UtcNow;
                var llmErrorMs = (llmEndError - llmStart).TotalMilliseconds;

                var detail = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                _log.LogWarning(
                    "AssistantService LLM call END (non-success) for {EquipmentId} after {DurationMs} ms with status {StatusCode}",
                    equipmentId,
                    llmErrorMs,
                    (int)httpResponse.StatusCode);

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

            var llmEnd = DateTime.UtcNow;
            var llmMs = (llmEnd - llmStart).TotalMilliseconds;
            _log.LogInformation(
                "AssistantService LLM call END for {EquipmentId} after {DurationMs} ms with status {StatusCode}",
                equipmentId,
                llmMs,
                (int)httpResponse.StatusCode);

            var overallEnd = DateTime.UtcNow;
            var overallMs = (overallEnd - overallStart).TotalMilliseconds;
            _log.LogInformation(
                "AssistantService.GetEquipmentAssistantResponseAsync EXIT for {EquipmentId} after {DurationMs} ms",
                equipmentId,
                overallMs);

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

        public async Task<AssistantResponseDto> BuildContextualAnswerAsync(
            AssistantAskRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ClusterMetrics cluster;
            if (!string.IsNullOrWhiteSpace(request.Equipment))
            {
                var timeline  = await _eventSummary.GetTimelineAsync(request.Equipment, hours: 24, cancellationToken);
                var lastStop  = await _eventSummary.GetLastStoppedAsync(request.Equipment, cancellationToken);
                cluster = ComputeClusterMetrics(timeline, lastStop);
            }
            else
            {
                cluster = new ClusterMetrics(false, null, false, null, false, null);
            }

            return new AssistantResponseDto
            {
                EquipmentId       = request.Equipment ?? string.Empty,
                AssistantResponse = BuildContextualAnswer(request, cluster),
                Availability1h    = request.Availability1h,
                Stops24h          = request.Stops24h,
                Alarms24h         = request.Alarms24h,
            };
        }

        // ── Cluster detection ────────────────────────────────────────────────────

        private sealed record ClusterMetrics(
            bool      HasCycling,
            DateTime? CyclingWindowStart,   // first event in first qualifying run-state window
            bool      HasAlarmBurst,
            DateTime? AlarmBurstStart,      // first event in first qualifying alarm window
            bool      HasRecentStop,
            DateTime? LastStopUtc
        );

        private static bool IsRunStateTransition(EquipmentEventTimelineItemDto e)
        {
            var type = e.EventType.ToLowerInvariant();
            var prev = (e.PreviousState ?? "").ToLowerInvariant();
            var next = e.NewState.ToLowerInvariant();
            return type.Contains("stop") || type.Contains("start") || type.Contains("state")
                || prev.Contains("running") || prev.Contains("stopped")
                || next.Contains("running") || next.Contains("stopped");
        }

        private static bool IsAlarmEvent(EquipmentEventTimelineItemDto e) =>
            e.EventType.ToLowerInvariant().Contains("alarm");

        /// <summary>
        /// Returns true and sets <paramref name="startUtc"/> to the first timestamp of the
        /// earliest qualifying cluster window (consecutive <paramref name="minCount"/> events
        /// within <paramref name="window"/>). Input must be sorted ascending.
        /// </summary>
        private static bool TryGetClusterStart(
            IList<DateTime> sorted, int minCount, TimeSpan window, out DateTime? startUtc)
        {
            startUtc = null;
            if (sorted.Count < minCount) return false;
            for (var i = 0; i <= sorted.Count - minCount; i++)
            {
                if (sorted[i + minCount - 1] - sorted[i] <= window)
                {
                    startUtc = sorted[i];
                    return true;
                }
            }
            return false;
        }

        private static ClusterMetrics ComputeClusterMetrics(
            IReadOnlyList<EquipmentEventTimelineItemDto> timeline,
            DateTime? lastStop)
        {
            var runTimes = timeline
                .Where(IsRunStateTransition)
                .Select(e => e.TimestampUtc)
                .OrderBy(t => t)
                .ToList();

            var alarmTimes = timeline
                .Where(IsAlarmEvent)
                .Select(e => e.TimestampUtc)
                .OrderBy(t => t)
                .ToList();

            var hasCycling    = TryGetClusterStart(runTimes,   minCount: 3, window: TimeSpan.FromMinutes(10), out var cyclingStart);
            var hasAlarmBurst = TryGetClusterStart(alarmTimes, minCount: 5, window: TimeSpan.FromMinutes(15), out var alarmStart);
            var hasRecentStop = lastStop.HasValue && (DateTime.UtcNow - lastStop.Value).TotalMinutes <= 5;

            return new ClusterMetrics(hasCycling, cyclingStart, hasAlarmBurst, alarmStart, hasRecentStop, lastStop);
        }

        // ── Response builder ──────────────────────────────────────────────────────

        private static string BuildContextualAnswer(AssistantAskRequestDto r, ClusterMetrics cluster)
        {
            var eq    = r.Equipment ?? "Unknown equipment";
            var lines = new List<string>();

            // ── Opening ──────────────────────────────────────────────────────────
            var availText = r.Availability1h.HasValue
                ? $"{r.Availability1h.Value:F1}%"
                : "unknown";
            lines.Add($"Diagnostic summary for {eq} (availability last 1h: {availText}).");

            if (!string.IsNullOrWhiteSpace(r.Issue))
                lines.Add($"Focus area: {r.Issue}.");

            if (!string.IsNullOrWhiteSpace(r.Sku))
                lines.Add($"Active SKU: {r.Sku}.");

            // ── Activity ─────────────────────────────────────────────────────────
            var activityParts = new List<string>();
            if (r.Stops24h.HasValue)  activityParts.Add($"{r.Stops24h.Value} stop(s)");
            if (r.Alarms24h.HasValue) activityParts.Add($"{r.Alarms24h.Value} alarm(s)");
            if (activityParts.Count > 0)
                lines.Add($"Activity in last 24h: {string.Join(", ", activityParts)}.");

            // ── Signal exceptions ─────────────────────────────────────────────────
            if (r.SignalExceptions is { Count: > 0 })
                lines.Add($"Signal exceptions: {string.Join("; ", r.SignalExceptions)}.");

            // ── Assessment ───────────────────────────────────────────────────────
            lines.Add(string.Empty);
            lines.Add("Assessment:");

            var findings = new List<string>();

            // ── Cluster findings (prepended before threshold findings) ────────────
            if (cluster.HasCycling)
            {
                var when = cluster.CyclingWindowStart.HasValue
                    ? $" beginning around {cluster.CyclingWindowStart.Value.ToLocalTime():HH:mm}"
                    : "";
                findings.Add($"Stop/start cycling detected{when} — investigate feeder, interlocks, or control loop instability.");
            }

            if (cluster.HasAlarmBurst)
            {
                var when = cluster.AlarmBurstStart.HasValue
                    ? $" beginning around {cluster.AlarmBurstStart.Value.ToLocalTime():HH:mm}"
                    : "";
                findings.Add($"Alarm burst detected{when} — likely shared upstream condition.");
            }

            if (cluster.HasRecentStop && cluster.LastStopUtc.HasValue)
                findings.Add($"Machine recently stopped at {cluster.LastStopUtc.Value.ToLocalTime():HH:mm} — check immediate cause before restart.");
            else if (!cluster.LastStopUtc.HasValue || (DateTime.UtcNow - cluster.LastStopUtc.Value).TotalHours >= 12)
                findings.Add("Machine stable over recent operating window.");

            // ── Threshold findings ────────────────────────────────────────────────
            if (r.Availability1h.HasValue)
            {
                var avail = r.Availability1h.Value;
                if (avail < 80)
                    findings.Add("Availability is critically low — investigate recent stop causes and check for recurring faults.");
                else if (avail < 90 && cluster.HasCycling)
                    findings.Add("Availability impact consistent with repeated stop/start cycling.");
                else if (avail < 90)
                    findings.Add("Availability is below target — review stop log for short-cycle or repeated stops.");
                else
                    findings.Add("Availability is within normal range.");
            }

            // Skip generic stop/alarm counts when cluster findings already explain them
            if (!cluster.HasCycling)
            {
                if (r.Stops24h.HasValue && r.Stops24h.Value >= 10)
                    findings.Add($"High stop frequency ({r.Stops24h.Value} in 24h) — check for interlock trips, material jams, or recurring fault codes.");
                else if (r.Stops24h.HasValue && r.Stops24h.Value >= 4)
                    findings.Add($"Elevated stop count ({r.Stops24h.Value} in 24h) — monitor for developing faults.");
            }

            if (!cluster.HasAlarmBurst && r.Alarms24h.HasValue && r.Alarms24h.Value >= 5)
                findings.Add($"Elevated alarm count ({r.Alarms24h.Value} in 24h) — review alarm log for repeating alarm codes.");

            if (r.SignalExceptions is { Count: > 0 })
                findings.Add($"{r.SignalExceptions.Count} signal exception(s) present — inspect highlighted sensors before next production run.");

            if (!string.IsNullOrWhiteSpace(r.Issue))
            {
                findings.Add(r.Issue switch
                {
                    "Frequent stops"          => "Prioritise stop-cause analysis; check upstream feeders and downstream buffer levels.",
                    "High alarm activity"     => "Pull the alarm frequency report and identify the top-repeating alarm code.",
                    "Low availability"        => "Compare scheduled vs. unscheduled downtime; verify shift handover notes.",
                    "Rising condition signal" => "Inspect the affected sensor and associated mechanical sub-assembly for wear or contamination.",
                    "Material risk"           => "Verify stock levels and incoming delivery schedule; check feeder path for jams.",
                    _                         => $"Review conditions related to: {r.Issue}."
                });
            }

            if (findings.Count == 0)
                findings.Add("No significant issues detected based on the provided context. Continue routine monitoring.");

            lines.AddRange(findings.Select((f, i) => $"{i + 1}. {f}"));

            return string.Join("\n", lines);
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
