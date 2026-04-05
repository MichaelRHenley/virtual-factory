(function () {
    const kpiTotalEl = document.getElementById("kpiTotal");
    const kpiRunningEl = document.getElementById("kpiRunning");
    const kpiAlarmedEl = document.getElementById("kpiAlarmed");
    const kpiAvailEl = document.getElementById("kpiAvailability");

    const worstTableBody = document.getElementById("worstTableBody");

    const drilldownSelect = document.getElementById("drilldownSelect");
    const ddRunStatusEl = document.getElementById("ddRunStatus");
    const ddAlarmStatusEl = document.getElementById("ddAlarmStatus");
    const ddAvailEl = document.getElementById("ddAvailability1h");
    const ddOrderInfoEl = document.getElementById("ddOrderInfo");

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

    const BADGE_SCORES = {
        "Material risk": 40,
        "Low availability": 30,
        "High alarm activity": 20,
        "Frequent stops": 15,
        "Rising condition signal": 10,
    };
    const BADGE_PRIORITY = [
        "Material risk", "Low availability", "High alarm activity",
        "Frequent stops", "Rising condition signal",
    ];

    function buildMaintenanceUrl(equipmentId, badgeLabel) {
        const params = new URLSearchParams();
        params.set("equipment", equipmentId);
        if (badgeLabel) params.set("issue", badgeLabel.toLowerCase().replace(/\s+/g, "-"));
        return `/maintenance.html?${params.toString()}`;
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
        lastRisingSegment = risingSegment;
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

    function severityScore(badges) {
        let score = 0;
        for (const b of badges) score += BADGE_SCORES[b] || 0;
        return score;
    }

    async function fetchEquipmentContextSafe(equipmentId) {
        try {
            const res = await fetch(`/api/equipment/${encodeURIComponent(equipmentId)}/context`);
            if (!res.ok) return {};
            return await res.json();
        } catch {
            return {};
        }
    }

    async function loadFleetAvailability() {
        try {
            const res = await fetch("/api/equipment/state-availability?hours=1");
            if (!res.ok) throw new Error("state-availability failed: " + res.status);
            const data = await res.json();
            if (!Array.isArray(data)) return;

            const total = data.length;
            let runningCount = 0;
            let alarmedCount = 0; // approximate from event summary via context fetch per machine if needed
            let avgAvail = 0;

            if (total > 0) {
                avgAvail = data.reduce((sum, x) => sum + (x.runningPercent || 0), 0) / total;
            }

            // runningCount: we approximate as machines with runningPercent > 0
            runningCount = data.filter(x => (x.runningPercent || 0) > 0).length;

            kpiTotalEl.textContent = String(total);
            kpiRunningEl.textContent = String(runningCount);
            kpiAlarmedEl.textContent = String(alarmedCount); // left as 0 unless enriched from other APIs
            kpiAvailEl.textContent = total > 0 ? avgAvail.toFixed(1) + "%" : "--";

            // Fetch context for every machine in parallel to compute diagnostic badges
            const ctxResults = await Promise.all(
                data.map(x => fetchEquipmentContextSafe(x.equipmentId))
            );

            // Enrich with badges and severity score; inject availability so badge check sees it
            const enriched = data.map((x, i) => {
                const ctx = Object.assign({}, ctxResults[i], { availability1h: x.runningPercent });
                const badges = computeBadges(ctx, "");
                return { ...x, badges, score: severityScore(badges) };
            });

            // Sort: severity score desc, then availability asc as tiebreaker
            enriched.sort((a, b) =>
                b.score !== a.score
                    ? b.score - a.score
                    : (a.runningPercent || 0) - (b.runningPercent || 0)
            );

            if (!enriched.length) {
                worstTableBody.innerHTML = "<tr><td colspan=\"3\">No data</td></tr>";
            } else {
                worstTableBody.innerHTML = enriched.map(x => {
                    const topBadge = BADGE_PRIORITY.find(b => x.badges.has(b)) || "";
                    const badgeHtml = BADGE_PRIORITY
                        .filter(b => x.badges.has(b))
                        .map(b => `<span class="badge fleet" data-badge="${escapeHtml(b)}">${escapeHtml(b)}</span>`)
                        .join("");
                    return `<tr data-eq="${escapeHtml(x.equipmentId)}" data-top-badge="${escapeHtml(topBadge)}">
                        <td>${escapeHtml(x.equipmentId)}</td>
                        <td>${(x.runningPercent || 0).toFixed(1)}%</td>
                        <td>${badgeHtml}</td>
                    </tr>`;
                }).join("");

                worstTableBody.querySelectorAll("tr").forEach(tr => {
                    // Row click: navigate to maintenance with top badge pre-activated
                    tr.addEventListener("click", () => {
                        const eq = tr.getAttribute("data-eq");
                        const topBadge = tr.getAttribute("data-top-badge") || null;
                        if (eq) window.location.href = buildMaintenanceUrl(eq, topBadge);
                    });
                    // Chip click: navigate with that specific badge; don't bubble to row
                    tr.querySelectorAll(".badge.fleet").forEach(chip =>
                        chip.addEventListener("click", e => {
                            e.stopPropagation();
                            const eq = tr.getAttribute("data-eq");
                            const badge = chip.getAttribute("data-badge") || null;
                            if (eq) window.location.href = buildMaintenanceUrl(eq, badge);
                        })
                    );
                });
            }

            if (lastUpdatedEl) {
                lastUpdatedEl.textContent = "Last updated: " + new Date().toLocaleTimeString();
            }
        } catch (err) {
            console.error("loadFleetAvailability failed", err);
            worstTableBody.innerHTML = "<tr><td colspan=\"3\">Failed to load</td></tr>";
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
                drilldownSelect.innerHTML = "<option>(no equipment)</option>";
                return;
            }

            drilldownSelect.innerHTML = names
                .map(n => `<option value="${escapeHtml(n)}">${escapeHtml(n)}</option>`)
                .join("");

            currentEquipment = names[0];
            await refreshDrilldown();
        } catch (err) {
            console.error("loadEquipmentList failed", err);
            drilldownSelect.innerHTML = "<option>(failed to load)</option>";
        }
    }

    async function setDrilldownEquipment(eq, openBadge) {
        if (!eq) return;
        currentEquipment = eq;
        if (drilldownSelect) {
            drilldownSelect.value = eq;
        }
        await refreshDrilldown();
        if (openBadge) onBadgeClick(openBadge);
    }

    async function refreshDrilldown() {
        if (!currentEquipment) return;
        await Promise.all([
            loadDrilldownContext(currentEquipment),
            loadDrilldownAvailability(currentEquipment)
        ]);
    }

    async function loadDrilldownContext(equipment) {
        try {
            const res = await fetch(`/api/equipment/${encodeURIComponent(equipment)}/context`);
            if (!res.ok) throw new Error("context failed: " + res.status);
            const ctx = await res.json();

            lastContext = ctx;

            const isRunning = ctx.runStatus === true || ctx.currentStatus === "Running";
            ddRunStatusEl.textContent = isRunning ? "Running" : (ctx.currentStatus || "Unknown");

            const hasAlarm = ctx.hasAlarm === true || (ctx.alarmState && String(ctx.alarmState).toLowerCase() === "active");
            ddAlarmStatusEl.textContent = hasAlarm ? "Alarm" : (ctx.alarmState || "Normal");

            const po = ctx.activeProductionOrder || ctx.currentProductionOrder || null;
            if (po) {
                const sku = po.sku || po.Sku || ctx.activeSku || ctx.currentSku || "";
                const orderId = po.orderId || po.OrderId || po.workOrderNumber || "";
                ddOrderInfoEl.textContent = `${orderId} / ${sku}`;
            } else {
                ddOrderInfoEl.textContent = "No active order";
            }

            if (lastUpdatedEl) {
                lastUpdatedEl.textContent = "Last updated: " + new Date().toLocaleTimeString();
            }
        } catch (err) {
            console.error("loadDrilldownContext failed", err);
        }
    }

    async function loadDrilldownAvailability(equipment) {
        try {
            const res = await fetch(`/api/equipment/${encodeURIComponent(equipment)}/state-availability?hours=1`);
            if (!res.ok) return;
            const data = await res.json();
            if (data && typeof data.runningPercent === "number") {
                ddAvailEl.textContent = data.runningPercent.toFixed(1) + "%";
                if (!lastContext) {
                    lastContext = {};
                }
                lastContext.availability1h = data.runningPercent / 100;
                updateBadgesFromAssistantContext(lastContext, null);
            }
        } catch (err) {
            console.error("loadDrilldownAvailability failed", err);
        }
    }

    function periodicRefresh() {
        if (refreshInProgress) return;
        refreshInProgress = true;
        const tasks = [loadFleetAvailability()];
        if (currentEquipment) tasks.push(refreshDrilldown());
        Promise.all(tasks).finally(() => { refreshInProgress = false; });
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
                lastContext = Object.assign({}, lastContext || {}, ctx);
                const raw = ctx.contextSummary || ctx.ContextSummary || ctx.inputSummary || ctx.InputSummary || "";
                updateBadgesFromAssistantContext(lastContext, raw);
                assistantResponseEl.textContent = formatSupervisorAssistantAnswer(raw);
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

        const badges = computeBadges(context || {}, assistantText);

        const sorted = Array.from(badges).sort((a, b) => {
            const ai = BADGE_PRIORITY.indexOf(a);
            const bi = BADGE_PRIORITY.indexOf(b);
            return (ai === -1 ? Infinity : ai) - (bi === -1 ? Infinity : bi);
        });
        // Only close the detail panel if the active badge no longer exists in the new set
        if (activeBadgeLabel && !sorted.includes(activeBadgeLabel)) {
            closeBadgeDetail();
        }
        if (!badges.size) {
            assistantBadgesEl.innerHTML = "";
            return;
        }
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

    function formatSupervisorAssistantAnswer(raw) {
        if (!raw) return "No response.";

        const currentCondition = extractAssistantSection(raw, "Current Condition");
        let riskAssessment = extractAssistantSection(raw, "Risk Assessment");
        const signalHealth = extractAssistantSection(raw, "Signal Health");
        const recentActivity = extractAssistantSection(raw, "Recent Activity");

        if (!riskAssessment && signalHealth) {
            riskAssessment = signalHealth;
        }

        const lines = [];
        if (currentCondition) lines.push("Current Condition: " + currentCondition.replace(/\s+/g, " "));
        if (riskAssessment) lines.push("Risk Assessment: " + riskAssessment.replace(/\s+/g, " "));
        if (recentActivity) lines.push("Recent Activity: " + recentActivity.replace(/\s+/g, " "));

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

    if (drilldownSelect) {
        drilldownSelect.addEventListener("change", async e => {
            const eq = e.target.value || null;
            if (eq) {
                await setDrilldownEquipment(eq);
            }
        });
    }

    if (askButton) {
        askButton.addEventListener("click", askAssistant);
    }

    // initial load; start polling after equipment list is populated so currentEquipment is set
    loadFleetAvailability();
    loadEquipmentList().then(() => startPolling());
})();
