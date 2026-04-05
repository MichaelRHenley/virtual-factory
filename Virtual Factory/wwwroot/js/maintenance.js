(function () {
    const equipmentSelect = document.getElementById("equipmentSelect");

    const runStatusEl = document.getElementById("runStatus");
    const alarmStatusEl = document.getElementById("alarmStatus");
    const availabilityEl = document.getElementById("availability1h");

    const orderInfoEl = document.getElementById("orderInfo");

    const signalNormalCountEl = document.getElementById("signalNormalCount");
    const signalNearCountEl = document.getElementById("signalNearCount");
    const signalAbnormalCountEl = document.getElementById("signalAbnormalCount");
    const signalExceptionListEl = document.getElementById("signalExceptionList");

    const recentStops24hEl = document.getElementById("recentStops24h");
    const recentAlarms24hEl = document.getElementById("recentAlarms24h");
    const recentLastAlarmEl = document.getElementById("recentLastAlarm");
    const recentLastStopEl = document.getElementById("recentLastStop");

    const pmOverdueEl = document.getElementById("pmOverdue");
    const pmUpcomingEl = document.getElementById("pmUpcoming");

    const askButton = document.getElementById("askButton");
    const assistantResponseEl = document.getElementById("assistantResponse");
    const lastUpdatedEl = document.getElementById("lastUpdatedTs");
    const activeIssuesEl = document.getElementById("activeIssues");
    const suggestedChecksEl = document.getElementById("suggestedChecks");
    const suggestedChecksContextEl = document.getElementById("suggestedChecksContext");
    const recentEventsEl = document.getElementById("recentEventsBody");
    const eventsFilterBarEl = document.getElementById("eventsFilterBar");
    const openInOperatorLinkEl = document.getElementById("openInOperatorLink");

    let currentEquipment = null;
    let lastContext = null;
    let lastEvents = [];
    let activeEventFilter = null;
    let refreshInProgress = false;
    let lastAutoAskTime = 0;
    let lastBadgeKey    = "";

    const BADGE_PRIORITY = [
        "Material risk", "Low availability", "High alarm activity",
        "Frequent stops", "Rising condition signal",
    ];

    const CHIP_SCROLL_TARGETS = {
        "Frequent stops":          "sectionRecentEvents",
        "High alarm activity":     "sectionRecentEvents",
        "Low availability":        "sectionRecentActivity",
        "Rising condition signal": "sectionSignalHealth",
        "Material risk":           "sectionOrderSku",
    };

    const SLUG_TO_BADGE = {
        "frequent-stops":          "Frequent stops",
        "high-alarm-activity":     "High alarm activity",
        "low-availability":        "Low availability",
        "rising-condition-signal": "Rising condition signal",
        "material-risk":           "Material risk",
    };

    const SUGGESTED_CHECKS = {
        "Material risk":          "Check feeder / material path / stock readiness",
        "Low availability":       "Inspect downtime causes",
        "High alarm activity":    "Review recurring alarms",
        "Frequent stops":         "Review stop frequency cluster",
        "Rising condition signal": "Inspect sensor trend",
    };

    function scheduleAutoAsk() {
        if (Date.now() - lastAutoAskTime < 10000) return;
        lastAutoAskTime = Date.now();
        askAssistant();
    }

    function buildOperatorUrl(equipmentId, badgeLabel) {
        const params = new URLSearchParams();
        params.set("equipment", equipmentId);
        if (badgeLabel) params.set("issue", badgeLabel.toLowerCase().replace(/\s+/g, "-"));
        return `/operator.html?${params.toString()}`;
    }

    function escapeHtml(value) {
        if (value == null) return "";
        return String(value)
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll("\"", "&quot;")
            .replaceAll("'", "&#39;");
    }

    function parseKeyValue(line) {
        const idx = line.indexOf(":");
        if (idx < 0) return null;
        const key = line.substring(0, idx).trim();
        const value = line.substring(idx + 1).trim();
        return { key, value };
    }

    function extractSectionByLabel(lines, label) {
        const prefix = label.toLowerCase();
        const match = lines.find(l => l.toLowerCase().startsWith(prefix));
        return match ? (parseKeyValue(match)?.value || null) : null;
    }

    function extractSignalHealthCounts(lines) {
        let normal = null, near = null, abnormal = null;
        lines.forEach(line => {
            const lower = line.toLowerCase().trim();
            if (lower.startsWith("normal:")) {
                normal = parseInt(line.split(":")[1], 10);
            } else if (lower.startsWith("near_limit:") || lower.startsWith("near-limit:")) {
                near = parseInt(line.split(":")[1], 10);
            } else if (lower.startsWith("abnormal:")) {
                abnormal = parseInt(line.split(":")[1], 10);
            }
        });
        return { normal, near, abnormal };
    }

    function extractSignalExceptionLines(lines) {
        const result = [];
        lines.forEach(line => {
            const trimmed = line.trim();
            if (!trimmed || trimmed.startsWith("signals:") || trimmed.startsWith("signal_health")) {
                return;
            }
            const lower = trimmed.toLowerCase();
            if (lower.includes("[nearlimit]") || lower.includes("nearlimit") || lower.includes("high") || lower.includes("low")) {
                // keep only the part before any '[' if present
                const namePart = trimmed.split("[")[0].trim();
                result.push(namePart);
            }
        });
        return result;
    }

    function computeBadges(ctx, assistantText) {
        const badges = new Set();
        const text = (assistantText || "").toLowerCase();

        const stopCount = ctx.stopCount24h ?? ctx.eventSummary?.stopCount24h ?? ctx.stopCountLast24Hours;
        if (typeof stopCount === "number" && stopCount >= 10) badges.add("Frequent stops");

        const alarmCount = ctx.alarmCount24h ?? ctx.eventSummary?.alarmCount24h ?? ctx.alarmCountLast24Hours;
        if (typeof alarmCount === "number" && alarmCount >= 5) badges.add("High alarm activity");

        const rawAvail = ctx.availability1h ?? ctx.availability1H;
        let avail;
        if (typeof rawAvail === "number")
            avail = rawAvail > 1 ? rawAvail / 100 : rawAvail;
        else if (rawAvail && typeof rawAvail.runningPercent === "number")
            avail = rawAvail.runningPercent / 100;
        if (typeof avail === "number" && avail < 0.90) badges.add("Low availability");

        const CONDITION_SIGNALS = ["temperature", "vibration", "pressure", "current", "torque"];
        const risingSignals = (ctx.keySignals || [])
            .filter(s => (s.trendDirection || s.TrendDirection || "").toLowerCase() === "rising")
            .map(s => (s.signalName || s.SignalName || "").toLowerCase());
        const risingIdx = text.indexOf("rising:");
        let risingSegment = "";
        if (risingIdx !== -1) {
            const after = text.slice(risingIdx + "rising:".length);
            const fallingIdx = after.indexOf("falling:");
            const periodIdx  = after.indexOf(".");
            const endIdx = fallingIdx !== -1 ? fallingIdx : periodIdx !== -1 ? periodIdx : after.length;
            risingSegment = after.slice(0, endIdx);
        }
        if (risingSignals.some(n => CONDITION_SIGNALS.some(c => n.includes(c))) ||
            CONDITION_SIGNALS.some(c => risingSegment.includes(c)))
            badges.add("Rising condition signal");

        const missingCount = Number(ctx.missingMaterials ?? ctx.operationalContext?.missingMaterials ?? 0);
        const matStatus = String(
            ctx.materialStatus?.stockStatus ?? ctx.operationalContext?.materialStatus?.stockStatus ?? ""
        ).toLowerCase();
        if ((Number.isFinite(missingCount) && missingCount > 0) ||
            ["low", "critical", "shortage", "insufficient"].some(s => matStatus === s) ||
            text.includes("material shortage") || text.includes("low material") || text.includes("not ready"))
            badges.add("Material risk");

        return badges;
    }

    function renderActiveIssues(badges) {
        if (!activeIssuesEl) return;
        const active = BADGE_PRIORITY.filter(b => badges.has(b));
        const newKey = active.join(",");
        if (newKey !== lastBadgeKey) {
            lastBadgeKey = newKey;
            scheduleAutoAsk();
        }
        if (!active.length) {
            activeIssuesEl.innerHTML = "<span style=\"color:#6b7280;font-size:0.85rem;\">No active issues detected.</span>";
            if (openInOperatorLinkEl && currentEquipment)
                openInOperatorLinkEl.href = buildOperatorUrl(currentEquipment, null);
            return;
        }
        activeIssuesEl.innerHTML = active
            .map(b => `<span class="badge fleet" data-badge="${escapeHtml(b)}">${escapeHtml(b)}</span>`)
            .join(" ");
        activeIssuesEl.querySelectorAll(".badge.fleet").forEach(chip =>
            chip.addEventListener("click", () => onIssueChipClick(chip.dataset.badge))
        );
        if (openInOperatorLinkEl && currentEquipment)
            openInOperatorLinkEl.href = buildOperatorUrl(currentEquipment, active[0]);
    }

    function onIssueChipClick(badge) {
        const targetId = CHIP_SCROLL_TARGETS[badge];
        if (targetId) {
            const el = document.getElementById(targetId);
            if (el) el.scrollIntoView({ behavior: "smooth", block: "start" });
        }
        if (badge === "Frequent stops" || badge === "High alarm activity") {
            activeEventFilter = badge;
            renderEventsTable();
        }
        scheduleAutoAsk();
    }

    function analyzeEvents(events) {
        if (!events || !events.length) return { latestEvent: null, clusterNote: null };

        // Latest event (sorted newest-first)
        const latest = events[0];
        const latestTs = latest.timestampUtc || latest.TimestampUtc;
        const latestTime = latestTs ? new Date(latestTs).toLocaleTimeString() : null;
        const latestType = latest.eventType || latest.EventType || "";
        const latestState = latest.newState || latest.NewState || "";
        const latestEvent = latestTime
            ? { time: latestTime, label: latestState ? `${latestType} \u2013 ${latestState}` : latestType }
            : null;

        // Stop cluster: 3+ stops within 15 minutes — scan all 24h events
        const WINDOW_MS = 15 * 60 * 1000;
        const stopTimes = events
            .filter(e => (e.eventType || e.EventType || "").toLowerCase().includes("stop"))
            .map(e => new Date(e.timestampUtc || e.TimestampUtc).getTime())
            .filter(t => !isNaN(t))
            .sort((a, b) => a - b);

        let best = { count: 0, spanMs: 0 };
        for (let i = 0; i < stopTimes.length; i++) {
            let j = i;
            while (j < stopTimes.length && stopTimes[j] - stopTimes[i] <= WINDOW_MS) j++;
            const count = j - i;
            if (count > best.count) best = { count, spanMs: stopTimes[j - 1] - stopTimes[i] };
        }

        // Repeated identical eventType: 3+ occurrences
        const typeCounts = {};
        events.forEach(e => {
            const t = (e.eventType || e.EventType || "").trim();
            if (t) typeCounts[t] = (typeCounts[t] || 0) + 1;
        });
        const topRepeated = Object.entries(typeCounts)
            .filter(([, n]) => n >= 3)
            .sort((a, b) => b[1] - a[1])[0] || null;

        // Stop cluster takes priority over generic repetition
        let clusterNote = null;
        if (best.count >= 3) {
            const mins = Math.round(best.spanMs / 60000);
            clusterNote = `Recent stop cluster detected: ${best.count} stops within ${mins} minute${mins !== 1 ? "s" : ""}`;
        } else if (topRepeated) {
            clusterNote = `Frequent stop/start cycling detected: ${topRepeated[1]} transitions in last 24 hours`;
        }

        return { latestEvent, clusterNote };
    }

    function renderSuggestedChecks(badges, eventAnalysis) {
        if (!suggestedChecksEl) return;
        const { latestEvent, clusterNote } = eventAnalysis || {};

        if (suggestedChecksContextEl) {
            suggestedChecksContextEl.textContent = latestEvent
                ? `Latest event: ${latestEvent.time} ${latestEvent.label}`
                : "";
        }

        const active = BADGE_PRIORITY.filter(b => badges.has(b));
        const items = [];

        if (clusterNote) {
            // Split at ": " so the label and detail sit on separate lines
            const colonIdx = clusterNote.indexOf(": ");
            if (colonIdx !== -1) {
                const label  = clusterNote.slice(0, colonIdx + 1);
                const detail = clusterNote.slice(colonIdx + 2);
                items.push(`<li class="check-cluster"><strong>${escapeHtml(label)}</strong><br>${escapeHtml(detail)}</li>`);
            } else {
                items.push(`<li class="check-cluster">${escapeHtml(clusterNote)}</li>`);
            }
        }

        if (!active.length && !clusterNote) {
            suggestedChecksEl.innerHTML = "<li style=\"color:#6b7280;\">No issues detected — routine inspection only.</li>";
            return;
        }

        items.push(...active.map(b =>
            `<li><strong>${escapeHtml(b)}:</strong> ${escapeHtml(SUGGESTED_CHECKS[b] || "—")}</li>`
        ));
        suggestedChecksEl.innerHTML = items.join("");
    }

    function renderEventsTable() {
        if (!recentEventsEl) return;

        let rows = lastEvents;
        let filterLabel = null;

        if (activeEventFilter === "Frequent stops") {
            rows = lastEvents.filter(e => {
                const type = (e.eventType     || e.EventType     || "").toLowerCase();
                const prev = (e.previousState || e.PreviousState || "").toLowerCase();
                const next = (e.newState      || e.NewState      || "").toLowerCase();
                return type.includes("stop")
                    || prev.includes("running") || next.includes("running")
                    || prev.includes("stopped") || next.includes("stopped");
            });
            filterLabel = "Frequent stops / run-state transitions";
        } else if (activeEventFilter === "High alarm activity") {
            rows = lastEvents.filter(e =>
                (e.eventType || e.EventType || "").toLowerCase().includes("alarm")
            );
            filterLabel = "Alarm events";
        }

        if (eventsFilterBarEl) {
            if (filterLabel) {
                eventsFilterBarEl.style.display = "";
                eventsFilterBarEl.innerHTML =
                    `<span class="filter-label">Filtering: ${escapeHtml(filterLabel)}</span>`
                  + `<button class="btn-link" id="clearEventsFilter">Show all events</button>`;
                document.getElementById("clearEventsFilter")?.addEventListener("click", () => {
                    activeEventFilter = null;
                    renderEventsTable();
                });
            } else {
                eventsFilterBarEl.style.display = "none";
                eventsFilterBarEl.innerHTML = "";
            }
        }

        if (!rows.length) {
            recentEventsEl.innerHTML = `<tr><td colspan="3">${filterLabel ? "No matching events" : "No events in last 24h"}</td></tr>`;
            return;
        }

        recentEventsEl.innerHTML = rows.slice(0, 10).map(ev => {
            const ts   = ev.timestampUtc   || ev.TimestampUtc;
            const time = ts ? new Date(ts).toLocaleTimeString() : "—";
            const type = escapeHtml(ev.eventType    || ev.EventType    || "—");
            const prev = ev.previousState  || ev.PreviousState;
            const next = ev.newState       || ev.NewState;
            const change = (prev && next) ? `${escapeHtml(prev)} → ${escapeHtml(next)}`
                         : next ? escapeHtml(next) : "—";
            return `<tr><td>${time}</td><td>${type}</td><td>${change}</td></tr>`;
        }).join("");
    }

    async function loadEvents(equipment) {
        if (!recentEventsEl) return;
        try {
            const res = await fetch(`/api/equipment/${encodeURIComponent(equipment)}/event-timeline?hours=24`);
            if (!res.ok) {
                lastEvents = [];
                recentEventsEl.innerHTML = "<tr><td colspan=\"3\">No data</td></tr>";
                return;
            }
            const events = await res.json();
            if (!Array.isArray(events) || !events.length) {
                lastEvents = [];
                renderEventsTable();
                return;
            }
            lastEvents = events
                .slice()
                .sort((a, b) => new Date(b.timestampUtc || b.TimestampUtc)
                              - new Date(a.timestampUtc || a.TimestampUtc));
            renderEventsTable();
        } catch (err) {
            console.error("loadEvents failed", err);
            recentEventsEl.innerHTML = "<tr><td colspan=\"3\">Failed to load</td></tr>";
        }
    }

    async function loadEquipmentList() {
        try {
            const res = await fetch("/api/assets/hierarchy");
            if (!res.ok) throw new Error("Failed to load hierarchy");

            const data = await res.json();
            const equipmentNames = new Set();

            (data || []).forEach(site => {
                (site.areas || []).forEach(area => {
                    (area.lines || []).forEach(line => {
                        (line.equipment || []).forEach(eq => {
                            if (eq && eq.equipmentName) {
                                equipmentNames.add(eq.equipmentName);
                            }
                        });
                    });
                });
            });

            const names = Array.from(equipmentNames).sort((a, b) => a.localeCompare(b));
            if (!names.length) {
                equipmentSelect.innerHTML = "<option>(no equipment)</option>";
                return;
            }

            equipmentSelect.innerHTML = names
                .map(n => `<option value="${escapeHtml(n)}">${escapeHtml(n)}</option>`)
                .join("");

            currentEquipment = names[0];
            await refreshForEquipment();
        } catch (err) {
            console.error("loadEquipmentList failed", err);
            if (equipmentSelect) {
                equipmentSelect.innerHTML = "<option>(failed to load)</option>";
            }
        }
    }

    async function refreshForEquipment() {
        if (!currentEquipment) return;

        await Promise.all([
            loadContext(currentEquipment),
            loadAvailability(currentEquipment),
            loadAssistantContext(currentEquipment),
            loadEvents(currentEquipment),
        ]);

        if (lastContext) {
            const badges = computeBadges(lastContext, "");
            const eventAnalysis = analyzeEvents(lastEvents);
            renderActiveIssues(badges);
            renderSuggestedChecks(badges, eventAnalysis);
        }
    }

    function periodicRefresh() {
        if (refreshInProgress || !currentEquipment) return;
        refreshInProgress = true;
        refreshForEquipment().finally(() => { refreshInProgress = false; });
    }

    function startPolling() {
        setInterval(periodicRefresh, 10000);
    }

    async function loadContext(equipment) {
        try {
            const res = await fetch(`/api/equipment/${encodeURIComponent(equipment)}/context`);
            if (!res.ok) throw new Error("context failed: " + res.status);
            const ctx = await res.json();
            lastContext = ctx;
            renderContext(ctx);
        } catch (err) {
            console.error("loadContext failed", err);
        }
    }

    function formatDuration(seconds) {
        if (seconds == null || seconds < 0) return "No data";
        if (seconds < 60) return `${seconds}s`;
        const m = Math.floor(seconds / 60);
        const s = seconds % 60;
        if (m < 60) return `${m}m ${s}s`;
        const h = Math.floor(m / 60);
        const rm = m % 60;
        return `${h}h ${rm}m`;
    }

    function renderContext(ctx) {
        if (!ctx) return;

        const isRunning = ctx.runStatus === true || ctx.currentStatus === "Running";
        runStatusEl.textContent = isRunning ? "Running" : (ctx.currentStatus || "No data");

        const hasAlarm = ctx.hasAlarm === true || (ctx.alarmState && String(ctx.alarmState).toLowerCase() === "active");
        alarmStatusEl.textContent = hasAlarm ? "Alarm" : (ctx.alarmState || "No data");

        const po = ctx.activeProductionOrder || ctx.currentProductionOrder || null;
        if (po) {
            const sku = po.sku || po.Sku || ctx.activeSku || ctx.currentSku || "";
            const orderId = po.orderId || po.OrderId || po.workOrderNumber || "";
            orderInfoEl.textContent = `${orderId} / ${sku}` || "No active order";
        } else {
            orderInfoEl.textContent = "No active order";
        }

        const lastAlarmAgo = ctx.lastAlarmSecondsAgo;
        const lastStoppedAgo = ctx.lastStoppedSecondsAgo;
        const stopCount24 = ctx.stopCount24h ?? ctx.stopCountLast24Hours;
        const alarmCount24 = ctx.alarmCount24h ?? ctx.alarmCountLast24Hours;

        recentStops24hEl.textContent = stopCount24 != null ? String(stopCount24) : "No data";
        recentAlarms24hEl.textContent = alarmCount24 != null ? String(alarmCount24) : "No data";
        recentLastAlarmEl.textContent = lastAlarmAgo != null ? formatDuration(lastAlarmAgo) + " ago" : "No data";
        recentLastStopEl.textContent = lastStoppedAgo != null ? formatDuration(lastStoppedAgo) + " ago" : "No data";

        const overdue = ctx.overdueMaintenanceCount ?? ctx.overduePmCount;
        const upcoming = ctx.upcomingMaintenanceCount ?? ctx.openPmCount;
        pmOverdueEl.textContent = overdue != null ? String(overdue) : "No data";
        pmUpcomingEl.textContent = upcoming != null ? String(upcoming) : "No data";

        if (lastUpdatedEl) {
            lastUpdatedEl.textContent = "Last updated: " + new Date().toLocaleTimeString();
        }
    }

    async function loadAvailability(equipment) {
        try {
            const res = await fetch(`/api/equipment/${encodeURIComponent(equipment)}/state-availability?hours=1`);
            if (!res.ok) return;
            const data = await res.json();
            if (data && typeof data.runningPercent === "number") {
                availabilityEl.textContent = data.runningPercent.toFixed(1) + "%";
                if (lastContext) lastContext.availability1h = data.runningPercent;
            } else {
                availabilityEl.textContent = "No data";
            }
        } catch (err) {
            console.error("loadAvailability failed", err);
            availabilityEl.textContent = "No data";
        }
    }

    async function loadAssistantContext(equipment) {
        try {
            const ctx = await window.AssistantClient.fetchAssistantContext(equipment);
            if (!ctx) {
                signalNormalCountEl.textContent = "No data";
                signalNearCountEl.textContent = "No data";
                signalAbnormalCountEl.textContent = "No data";
                signalExceptionListEl.innerHTML = "";
                return;
            }
            const input = ctx.inputSummary || "";

            const lines = input.split(/\r?\n/).map(l => l.trim()).filter(l => l.length > 0);

            const counts = extractSignalHealthCounts(lines);
            signalNormalCountEl.textContent = counts.normal != null ? String(counts.normal) : "No data";
            signalNearCountEl.textContent = counts.near != null ? String(counts.near) : "No data";
            signalAbnormalCountEl.textContent = counts.abnormal != null ? String(counts.abnormal) : "No data";

            const exceptionLines = extractSignalExceptionLines(lines).slice(0, 8);
            if (!exceptionLines.length) {
                signalExceptionListEl.innerHTML = "";
                const li = document.createElement("li");
                li.textContent = "No near-limit or abnormal signals.";
                signalExceptionListEl.appendChild(li);
            } else {
                signalExceptionListEl.innerHTML = exceptionLines
                    .map(x => `<li>${escapeHtml(x)}</li>`)
                    .join("");
            }
        } catch (err) {
            console.error("loadAssistantContext failed", err);
        }
    }

    async function askAssistant() {
        if (!currentEquipment) return;
        askButton.disabled = true;
        askButton.textContent = "Thinking...";
        assistantResponseEl.textContent = "Thinking...";
        try {
            const ctx = lastContext || {};
            const issueSlug = new URLSearchParams(window.location.search).get("issue");
            const issue = issueSlug ? (SLUG_TO_BADGE[issueSlug] || issueSlug) : null;

            const rawAvail = ctx.availability1h ?? ctx.availability1H;
            let avail = null;
            if (typeof rawAvail === "number")
                avail = rawAvail > 1 ? rawAvail : rawAvail * 100;
            else if (rawAvail && typeof rawAvail.runningPercent === "number")
                avail = rawAvail.runningPercent;

            const po = ctx.activeProductionOrder || ctx.currentProductionOrder || null;
            const sku = po?.sku || po?.Sku || ctx.activeSku || ctx.currentSku || null;

            const signalExceptions = Array.from(
                signalExceptionListEl?.querySelectorAll("li") || []
            ).map(li => li.textContent.trim())
             .filter(t => t && t !== "No near-limit or abnormal signals.");

            const payload = {
                equipment:       currentEquipment,
                issue:           issue,
                availability1h:  avail,
                stops24h:        ctx.eventSummary?.stopCount24h  ?? ctx.stopCount24h  ?? null,
                alarms24h:       ctx.eventSummary?.alarmCount24h ?? ctx.alarmCount24h ?? null,
                sku:             sku,
                signalExceptions: signalExceptions.length ? signalExceptions : null,
            };

            const res = await fetch("/api/assistant/ask", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload),
            });
            if (!res.ok) throw new Error("ask failed: " + res.status);
            const data = await res.json();
            const raw = data.answer ?? data.assistantResponse ?? data.response ?? "No response.";
            renderAssistantAnswer(raw, assistantResponseEl, data);
        } catch (err) {
            console.error("askAssistant failed", err);
            assistantResponseEl.textContent = "Assistant request failed.";
        } finally {
            askButton.disabled = false;
            askButton.textContent = "Ask Assistant";
        }
    }

    function extractAssistantSection(text, heading) {
        if (!text || !heading) return null;
        const lines = text.split(/\r?\n/);
        const target = `### ${heading}`.toLowerCase();
        let inSection = false;
        const collected = [];

        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            const trimmed = line.trim();
            if (!inSection) {
                if (trimmed.toLowerCase() === target) {
                    inSection = true;
                }
                continue;
            }

            if (/^###\s+/.test(trimmed)) {
                break;
            }
            collected.push(line);
        }

        const textBlock = collected.join("\n").trim();
        return textBlock.length ? textBlock : null;
    }

    function renderAssistantAnswer(raw, el, data = {}) {
        if (!el) return;
        if (!raw || !raw.trim()) { el.textContent = "No response."; return; }

        const lines = raw.split(/\r?\n/);
        const assessIdx = lines.findIndex(l => l.trim() === "Assessment:");

        // No recognised structure — plain-text fallback
        if (assessIdx === -1) {
            el.style.whiteSpace = "pre-wrap";
            el.textContent = raw;
            return;
        }

        const esc = s => String(s)
            .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");

        const headerLines  = lines.slice(0, assessIdx).map(l => l.trim()).filter(Boolean);
        const assessLines  = lines.slice(assessIdx + 1).map(l => l.trim()).filter(Boolean);

        const ACTIVITY_PREFIXES = ["Activity in last 24h:", "Signal exceptions:"];
        const summaryLines  = headerLines.filter(l => !ACTIVITY_PREFIXES.some(p => l.startsWith(p)));
        const activityLines = headerLines.filter(l =>  ACTIVITY_PREFIXES.some(p => l.startsWith(p)));

        // Prefer structured fields from the response for metric rows
        const hasStructured = data.availability1h != null || data.stops24h != null || data.alarms24h != null;
        const metricRows = hasStructured ? [
            data.availability1h != null ? { label: "Availability (1h)", value: `${Number(data.availability1h).toFixed(1)}%` } : null,
            data.stops24h  != null ? { label: "Stops (24h)",  value: String(data.stops24h)  } : null,
            data.alarms24h != null ? { label: "Alarms (24h)", value: String(data.alarms24h) } : null,
        ].filter(Boolean) : null;

        const numberedItems  = assessLines.filter(l =>  /^\d+\.\s/.test(l)).map(l => l.replace(/^\d+\.\s*/, ""));
        const freeformAssess = assessLines.filter(l => !/^\d+\.\s/.test(l));

        const LABEL   = "font-size:0.72rem;text-transform:uppercase;letter-spacing:.05em;color:#666;margin-bottom:3px;";
        const SECTION = "margin-bottom:10px;";
        const ROW     = "display:flex;justify-content:space-between;font-size:0.85rem;margin-top:2px;";

        let html = "";

        if (summaryLines.length)
            html += `<div style="${SECTION}"><div style="${LABEL}">Summary</div>`
                  + summaryLines.map(l => `<div>${esc(l)}</div>`).join("")
                  + `</div>`;

        if (metricRows)
            // Structured: individual labeled metric rows
            html += `<div style="${SECTION}"><div style="${LABEL}">Activity</div>`
                  + metricRows.map(m =>
                        `<div style="${ROW}">` +
                        `<span style="color:#555;">${esc(m.label)}</span>` +
                        `<span style="font-weight:600;">${esc(m.value)}</span>` +
                        `</div>`
                    ).join("")
                  + `</div>`;
        else if (activityLines.length)
            // Fallback: render raw activity text line(s)
            html += `<div style="${SECTION}"><div style="${LABEL}">Activity</div>`
                  + activityLines.map(l => `<div style="color:#555;">${esc(l)}</div>`).join("")
                  + `</div>`;

        if (numberedItems.length || freeformAssess.length) {
            html += `<div><div style="${LABEL}">Assessment</div>`;
            if (numberedItems.length)
                html += `<ol style="margin:0;padding-left:18px;">`
                      + numberedItems.map(item => `<li style="margin-bottom:2px;">${esc(item)}</li>`).join("")
                      + `</ol>`;
            if (freeformAssess.length)
                html += freeformAssess.map(l => `<div>${esc(l)}</div>`).join("");
            html += `</div>`;
        }

        el.style.whiteSpace = "normal";
        el.innerHTML = html;
    }

    async function activateFromUrl() {
        const params = new URLSearchParams(window.location.search);
        const equipParam = params.get("equipment");
        const issueParam  = params.get("issue");

        if (equipParam && equipmentSelect) {
            const option = Array.from(equipmentSelect.options).find(o => o.value === equipParam);
            if (option) {
                equipmentSelect.value = equipParam;
                currentEquipment = equipParam;
                activeEventFilter = null;
                await refreshForEquipment();
            }
        }

        if (issueParam) {
            const badgeLabel = SLUG_TO_BADGE[issueParam];
            if (badgeLabel) onIssueChipClick(badgeLabel);
        }
    }

    if (equipmentSelect) {
        equipmentSelect.addEventListener("change", async e => {
            currentEquipment  = e.target.value || null;
            activeEventFilter = null;
            lastAutoAskTime   = 0;
            lastBadgeKey      = "";
            await refreshForEquipment();
        });
    }

    if (askButton) {
        askButton.addEventListener("click", askAssistant);
    }

    if (equipmentSelect) {
        loadEquipmentList().then(() => activateFromUrl()).then(() => startPolling());
    }
})();
