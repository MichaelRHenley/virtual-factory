(function () {
    const equipmentSelect = document.getElementById("equipmentSelect");
    const runStatusEl = document.getElementById("runStatus");
    const alarmStatusEl = document.getElementById("alarmStatus");
    const availabilityEl = document.getElementById("availability1h");
    const orderInfoEl = document.getElementById("orderInfo");
    const orderProgressTextEl = document.getElementById("orderProgressText");
    const orderProgressFillEl = document.getElementById("orderProgressFill");
    const askButton = document.getElementById("askButton");
    const assistantBadgesEl = document.getElementById("assistantBadges");
    const assistantResponseEl = document.getElementById("assistantResponse");
    const badgeDetailEl = document.getElementById("badgeDetail");
    const lastUpdatedEl = document.getElementById("lastUpdatedTs");

    let currentEquipment = null;
    let lastContext = null;
    let activeBadgeLabel = null;
    let lastRisingSegment = "";
    let refreshInProgress = false;

    function escapeHtml(value) {
        if (value == null) return "";
        return String(value)
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll("\"", "&quot;")
            .replaceAll("'", "&#39;");
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
            loadAvailability(currentEquipment)
        ]);
    }

    async function loadContext(equipment) {
        try {
            const res = await fetch(`/api/equipment/${encodeURIComponent(equipment)}/context`);
            if (!res.ok) throw new Error("context failed: " + res.status);
            const ctx = await res.json();
            // snapshot tracked fields from previous context before overwriting
            const prev = {
                availability1h:  lastContext?.availability1h,
                stopCount24h:    lastContext?.eventSummary?.stopCount24h  ?? lastContext?.stopCount24h,
                alarmCount24h:   lastContext?.eventSummary?.alarmCount24h ?? lastContext?.alarmCount24h,
                lastStopTime:    lastContext?.lastStoppedUtc ?? lastContext?.eventSummary?.latestEvent?.startTimeUtc,
                lastAlarmTime:   lastContext?.lastAlarmUtc,
            };
            lastContext = ctx;
            renderContext(ctx);
            logContextDiff(prev, ctx);
        } catch (err) {
            console.error("loadContext failed", err);
        }
    }

    function logContextDiff(prev, next) {
        const fields = {
            "availability1h":             [prev.availability1h,  next.availability1h],
            "eventSummary.stopCount24h":  [prev.stopCount24h,    next.eventSummary?.stopCount24h  ?? next.stopCount24h],
            "eventSummary.alarmCount24h": [prev.alarmCount24h,   next.eventSummary?.alarmCount24h ?? next.alarmCount24h],
            "lastStopTime":               [prev.lastStopTime,    next.lastStoppedUtc ?? next.eventSummary?.latestEvent?.startTimeUtc],
            "lastAlarmTime":              [prev.lastAlarmTime,   next.lastAlarmUtc],
        };
        const changed = Object.entries(fields).filter(([, [a, b]]) => a !== b);
        if (changed.length) {
            console.log("[refresh] context changed:", Object.fromEntries(
                changed.map(([k, [a, b]]) => [k, { from: a, to: b }])
            ));
        } else {
            console.log("[refresh] context unchanged");
        }
    }

    function renderContext(ctx) {
        if (!ctx) return;

        const isRunning = ctx.runStatus === true || ctx.currentStatus === "Running";
        runStatusEl.textContent = isRunning ? "Running" : (ctx.currentStatus || "Unknown");

        const hasAlarm = ctx.hasAlarm === true || (ctx.alarmState && String(ctx.alarmState).toLowerCase() === "active");
        alarmStatusEl.textContent = hasAlarm ? "Alarm" : (ctx.alarmState || "Normal");

        const po = ctx.activeProductionOrder || ctx.currentProductionOrder || null;
        if (po) {
            const sku = po.sku || po.Sku || ctx.activeSku || ctx.currentSku || "";
            orderInfoEl.textContent = `${po.orderId || po.OrderId || po.workOrderNumber || ""} / ${sku}`;

            const planned = po.plannedQuantity ?? po.PlannedQuantity ?? 0;
            const completed = po.completedQuantity ?? po.CompletedQuantity ?? 0;
            let pct = 0;
            if (planned > 0) pct = Math.min(100, Math.max(0, (completed / planned) * 100));
            orderProgressTextEl.textContent = planned > 0
                ? `${completed} / ${planned} (${pct.toFixed(1)}%)`
                : "--";
            orderProgressFillEl.style.width = pct.toFixed(1) + "%";
        } else {
            orderInfoEl.textContent = "No active order";
            orderProgressTextEl.textContent = "--";
            orderProgressFillEl.style.width = "0%";
        }

        // update badges based on latest context
        updateBadgesFromAssistantContext(ctx);

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
                if (!lastContext) {
                    lastContext = {};
                }
                lastContext.availability1h = data.runningPercent;
                updateBadgesFromAssistantContext(lastContext);
            }
        } catch (err) {
            console.error("loadAvailability failed", err);
        }
    }

    function periodicRefresh() {
        if (refreshInProgress || !currentEquipment) return;
        refreshInProgress = true;
        refreshForEquipment().finally(() => { refreshInProgress = false; });
    }

    function startPolling() {
        setInterval(periodicRefresh, 5000);
    }

    async function askAssistant() {
        if (!currentEquipment) return;
        askButton.disabled = true;
        askButton.textContent = "Thinking...";
        assistantResponseEl.textContent = "Thinking...";
        try {
            const ctx = await window.AssistantClient.fetchAssistantContext(currentEquipment);
            if (!ctx) {
                assistantResponseEl.textContent = "No assistant context available.";
            } else {
                // keep a copy of assistant context for badge heuristics
                lastContext = Object.assign({}, lastContext || {}, ctx);
                updateBadgesFromAssistantContext(lastContext, ctx.contextSummary || ctx.ContextSummary || ctx.inputSummary || ctx.InputSummary || "");
                const raw = ctx.contextSummary || ctx.ContextSummary || ctx.inputSummary || ctx.InputSummary || "";
                assistantResponseEl.textContent = formatOperatorAssistantAnswer(raw);
            }
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

    function updateBadgesFromAssistantContext(context, assistantText) {
        if (!assistantBadgesEl) return;

        const ctx = context || {};
        const badges = new Set();
        const text = (assistantText || "").toLowerCase();

        // ── 1. Frequent stops ────────────────────────────────────────────────
        const stopCount = ctx.stopCount24h
                       ?? ctx.eventSummary?.stopCount24h
                       ?? ctx.stopCountLast24Hours;
        if (typeof stopCount === "number" && stopCount >= 10) {
            badges.add("Frequent stops");
        }

        // ── 2. High alarm activity ───────────────────────────────────────────
        const alarmCount = ctx.alarmCount24h
                        ?? ctx.eventSummary?.alarmCount24h
                        ?? ctx.alarmCountLast24Hours;
        if (typeof alarmCount === "number" && alarmCount >= 5) {
            badges.add("High alarm activity");
        }

        // ── 3. Low availability — normalise to 0-1 regardless of source ─────
        // availability1h may be a plain number (0-100 from equipment context),
        // already divided (0-1 stored after the availability endpoint),
        // or an EquipmentStateAvailabilityDto object (from assistant context).
        const rawAvail = ctx.availability1h ?? ctx.availability1H;
        let avail;
        if (typeof rawAvail === "number") {
            avail = rawAvail > 1 ? rawAvail / 100 : rawAvail;
        } else if (rawAvail && typeof rawAvail.runningPercent === "number") {
            avail = rawAvail.runningPercent / 100;
        }
        if (typeof avail === "number" && avail < 0.90) {
            badges.add("Low availability");
        }

        // ── 4. Rising condition signal ────────────────────────────────────────
        // Prefer structured keySignals[].trendDirection; fall back to text.
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
            const endIdx = fallingIdx !== -1 ? fallingIdx
                         : periodIdx  !== -1 ? periodIdx
                         : after.length;
            risingSegment = after.slice(0, endIdx);
        }
        lastRisingSegment = risingSegment;
        const hasRisingCondition =
            risingSignals.some(name => CONDITION_SIGNALS.some(c => name.includes(c))) ||
            CONDITION_SIGNALS.some(c => risingSegment.includes(c));

        if (hasRisingCondition) {
            badges.add("Rising condition signal");
        }

        // ── 5. Material risk ─────────────────────────────────────────────────
        // Prefer structured fields; fall back to text.
        const missingMaterialCount = Number(
            ctx.missingMaterials ?? ctx.operationalContext?.missingMaterials ?? 0
        );
        const matStatus = String(
            ctx.materialStatus?.stockStatus ??
            ctx.operationalContext?.materialStatus?.stockStatus ??
            ""
        ).toLowerCase();
        const MATERIAL_RISK_STATUSES = ["low", "critical", "shortage", "insufficient"];

        const hasMaterialRisk =
            (Number.isFinite(missingMaterialCount) && missingMaterialCount > 0) ||
            MATERIAL_RISK_STATUSES.some(s => matStatus === s) ||
            text.includes("material shortage") ||
            text.includes("low material") ||
            text.includes("not ready");

        if (hasMaterialRisk) {
            badges.add("Material risk");
        }

        renderBadges(Array.from(badges));
    }

    function renderBadges(badges) {
        if (!assistantBadgesEl) return;
        // Only close the detail panel if the active badge no longer exists in the new set
        if (activeBadgeLabel && !badges.includes(activeBadgeLabel)) {
            closeBadgeDetail();
        }
        if (!badges || !badges.length) {
            assistantBadgesEl.innerHTML = "";
            return;
        }
        const BADGE_PRIORITY = [
            "Material risk", "Low availability", "High alarm activity",
            "Frequent stops", "Rising condition signal",
        ];
        const sorted = [...badges].sort((a, b) => {
            const ai = BADGE_PRIORITY.indexOf(a);
            const bi = BADGE_PRIORITY.indexOf(b);
            return (ai === -1 ? Infinity : ai) - (bi === -1 ? Infinity : bi);
        });
        assistantBadgesEl.innerHTML = sorted
            .map(label => `<button class="badge warning" data-badge="${escapeHtml(label)}">${escapeHtml(label)}</button>`)
            .join(" ");
        // Re-apply active highlight after innerHTML replacement wipes all classes
        if (activeBadgeLabel) {
            assistantBadgesEl.querySelectorAll(".badge").forEach(btn =>
                btn.classList.toggle("active", btn.dataset.badge === activeBadgeLabel)
            );
        }
        assistantBadgesEl.querySelectorAll(".badge").forEach(btn =>
            btn.addEventListener("click", () => onBadgeClick(btn.dataset.badge))
        );
    }

    function onBadgeClick(label) {
        if (activeBadgeLabel === label) {
            closeBadgeDetail();
            return;
        }
        activeBadgeLabel = label;
        assistantBadgesEl.querySelectorAll(".badge").forEach(btn =>
            btn.classList.toggle("active", btn.dataset.badge === label)
        );
        if (badgeDetailEl) {
            badgeDetailEl.textContent = buildBadgeDetail(label, lastContext || {});
            badgeDetailEl.classList.add("open");
        }
    }

    function closeBadgeDetail() {
        activeBadgeLabel = null;
        assistantBadgesEl?.querySelectorAll(".badge").forEach(btn => btn.classList.remove("active"));
        if (badgeDetailEl) {
            badgeDetailEl.textContent = "";
            badgeDetailEl.classList.remove("open");
        }
    }

    function formatRelativeTime(date) {
        if (!date || isNaN(date)) return null;
        const mins = Math.floor((Date.now() - date.getTime()) / 60000);
        if (mins < 90)  return `${mins}m ago`;
        const hours = Math.floor(mins / 60);
        if (hours < 48) return `${hours}h ago`;
        return `${Math.floor(hours / 24)}d ago`;
    }

    function buildBadgeDetail(label, ctx) {
        const CONDITION_SIGNALS = ["temperature", "vibration", "pressure", "current", "torque"];

        switch (label) {
            case "Frequent stops": {
                const count = ctx.eventSummary?.stopCount24h ?? ctx.stopCount24h ?? "—";
                const rawTime = ctx.lastStoppedUtc || ctx.LastStoppedUtc
                             || ctx.eventSummary?.latestEvent?.startTimeUtc;
                const ago = rawTime ? formatRelativeTime(new Date(rawTime)) : null;
                const severity = typeof count === "number"
                    ? count >= 50 ? "high" : count >= 20 ? "moderate" : "elevated"
                    : null;
                const lines = [`Stops (24h): ${count}`];
                if (ago)      lines.push(`Last stop: ${ago}`);
                if (severity) lines.push(`Stop frequency: ${severity}`);
                return lines.join("\n");
            }
            case "High alarm activity": {
                const count = ctx.eventSummary?.alarmCount24h ?? ctx.alarmCount24h ?? "—";
                const rawTime = ctx.lastAlarmUtc || ctx.LastAlarmUtc
                             || ctx.eventSummary?.latestEvent?.startTimeUtc;
                const ago = rawTime ? formatRelativeTime(new Date(rawTime)) : null;
                const density = typeof count === "number"
                    ? count >= 20 ? "high" : count >= 8 ? "elevated" : "moderate"
                    : null;
                const lines = [`Alarms (24h): ${count}`];
                if (ago)     lines.push(`Last alarm: ${ago}`);
                if (density) lines.push(`Alarm density: ${density}`);
                return lines.join("\n");
            }
            case "Low availability": {
                const rawAvail = ctx.availability1h ?? ctx.availability1H;
                let pct;
                if (typeof rawAvail === "number")
                    pct = rawAvail > 1 ? rawAvail : rawAvail * 100;
                else if (rawAvail && typeof rawAvail.runningPercent === "number")
                    pct = rawAvail.runningPercent;
                const lines = pct != null
                    ? [`Availability (1h): ${pct.toFixed(1)}%`]
                    : ["Availability (1h): —"];
                const stopCount = ctx.eventSummary?.stopCount24h ?? ctx.stopCount24h;
                if (typeof stopCount === "number") lines.push(`Stops (24h): ${stopCount}`);
                const alarmCount = ctx.eventSummary?.alarmCount24h ?? ctx.alarmCount24h;
                if (typeof alarmCount === "number") lines.push(`Alarms (24h): ${alarmCount}`);
                const topRising = (ctx.keySignals || [])
                    .filter(s => (s.trendDirection || s.TrendDirection || "").toLowerCase() === "rising")
                    .map(s => s.signalName || s.SignalName)
                    .filter(Boolean)[0];
                if (topRising) lines.push(`Top signal change: ${topRising}`);
                return lines.join("\n");
            }
            case "Rising condition signal": {
                // Structured: signal name + current value
                const risingSignals = (ctx.keySignals || [])
                    .filter(s => (s.trendDirection || s.TrendDirection || "").toLowerCase() === "rising")
                    .filter(s => {
                        const name = (s.signalName || s.SignalName || "").toLowerCase();
                        return CONDITION_SIGNALS.some(c => name.includes(c));
                    });
                if (risingSignals.length) {
                    const bullets = risingSignals.map(s => {
                        const name = s.signalName || s.SignalName;
                        const val  = s.value      || s.Value;
                        return val ? `• ${name}: ${val}` : `• ${name}`;
                    });
                    return "Rising signals:\n" + bullets.join("\n");
                }
                // Text fallback
                if (lastRisingSegment.trim()) {
                    const parsed = lastRisingSegment.split(/[,;]/)
                        .map(s => s.trim())
                        .filter(s => s && CONDITION_SIGNALS.some(c => s.includes(c)));
                    const items = parsed.length ? parsed : [lastRisingSegment.trim()];
                    return "Rising signals:\n" + items.map(n => `• ${n}`).join("\n");
                }
                return "Rising condition detected in assistant summary.";
            }
            case "Material risk": {
                const lines = [];
                const matStatus = String(
                    ctx.materialStatus?.stockStatus ??
                    ctx.operationalContext?.materialStatus?.stockStatus ?? ""
                ).toLowerCase();
                if (matStatus) lines.push(`Stock status: ${matStatus}`);
                const missingCount = Number(
                    ctx.missingMaterials ?? ctx.operationalContext?.missingMaterials ?? 0
                );
                if (Number.isFinite(missingCount) && missingCount > 0)
                    lines.push(`Missing materials: ${missingCount}`);
                const readiness = (Number.isFinite(missingCount) && missingCount > 0)
                    ? "blocked"
                    : ["low", "critical"].includes(matStatus) ? "at risk"
                    : "degraded";
                lines.push(`Production readiness: ${readiness}`);
                return lines.length ? lines.join("\n") : "Material risk detected";
            }
            default:
                return "";
        }
    }

    function formatOperatorAssistantAnswer(raw) {
        if (!raw) return "No response.";

        const currentCondition = extractAssistantSection(raw, "Current Condition");
        const operationalContext = extractAssistantSection(raw, "Operational Context");
        let suggestedChecks = extractAssistantSection(raw, "Suggested Checks");

        if (suggestedChecks) {
            const bulletLines = suggestedChecks
                .split(/\r?\n/)
                .map(l => l.trim())
                .filter(l => l.length > 0 && (l.startsWith("-") || l.startsWith("*") || l.match(/^\d+\./)));
            if (bulletLines.length > 0) {
                suggestedChecks = bulletLines[0];
            }
        }

        const lines = [];
        if (currentCondition) lines.push("Current Condition: " + currentCondition.replace(/\s+/g, " "));
        if (operationalContext) lines.push("Operational Context: " + operationalContext.replace(/\s+/g, " "));
        if (suggestedChecks) lines.push("Suggested Check: " + suggestedChecks.replace(/^[*-]\s*/, ""));

        if (!lines.length) {
            const fallbackLines = raw.split(/\r?\n/)
                .map(l => l.trim())
                .filter(l => l.length > 0)
                .slice(0, 4);
            return fallbackLines.join("\n");
        }

        return lines
            .map(l => (l.length > 260 ? l.substring(0, 257) + "..." : l))
            .join("\n");
    }

    if (equipmentSelect) {
        equipmentSelect.addEventListener("change", async e => {
            currentEquipment = e.target.value || null;
            await refreshForEquipment();
        });
    }

    if (askButton) {
        askButton.addEventListener("click", askAssistant);
    }

    if (equipmentSelect) {
        loadEquipmentList().then(() => startPolling());
    }
})();
