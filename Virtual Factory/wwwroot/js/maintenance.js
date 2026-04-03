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

    let currentEquipment = null;

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
            loadAssistantContext(currentEquipment)
        ]);
    }

    async function loadContext(equipment) {
        try {
            const res = await fetch(`/api/equipment/${encodeURIComponent(equipment)}/context`);
            if (!res.ok) throw new Error("context failed: " + res.status);
            const ctx = await res.json();
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
    }

    async function loadAvailability(equipment) {
        try {
            const res = await fetch(`/api/equipment/${encodeURIComponent(equipment)}/state-availability?hours=1`);
            if (!res.ok) return;
            const data = await res.json();
            if (data && typeof data.runningPercent === "number") {
                availabilityEl.textContent = data.runningPercent.toFixed(1) + "%";
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
            const res = await fetch(`/api/assistant/context/${encodeURIComponent(equipment)}`);
            if (!res.ok) {
                signalNormalCountEl.textContent = "No data";
                signalNearCountEl.textContent = "No data";
                signalAbnormalCountEl.textContent = "No data";
                signalExceptionListEl.innerHTML = "";
                return;
            }
            const ctx = await res.json();
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
            const res = await fetch("/api/assistant/ask", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ equipmentName: currentEquipment })
            });
            if (!res.ok) {
                assistantResponseEl.textContent = `Assistant error: ${res.status}`;
                return;
            }
            const data = await res.json();
            const raw = data.answer ?? data.assistantResponse ?? "No response.";
            assistantResponseEl.textContent = formatMaintenanceAssistantAnswer(raw);
        } catch (err) {
            console.error("askAssistant failed", err);
            assistantResponseEl.textContent = "Assistant request failed.";
        } finally {
            askButton.disabled = false;
            askButton.textContent = "Ask Assistant";
        }
    }

    function splitIntoSections(raw) {
        if (!raw) return { __all: "" };
        const lines = raw.split(/\r?\n/);
        const sections = {};
        let current = "__preamble";
        sections[current] = [];

        lines.forEach(line => {
            const trimmed = line.trim();
            if (!trimmed) return;

            const m = /^#+\s*(.+)$/.exec(trimmed);
            if (m) {
                current = m[1].trim().toLowerCase();
                if (!sections[current]) sections[current] = [];
            } else {
                sections[current].push(trimmed);
            }
        });

        const result = {};
        Object.keys(sections).forEach(k => {
            result[k] = sections[k].join("\n").trim();
        });
        result.__all = raw.trim();
        return result;
    }

    function pickFirstNonEmpty(sections, keys) {
        for (const k of keys) {
            const v = sections[k.toLowerCase()];
            if (v && v.trim().length > 0) return v.trim();
        }
        return null;
    }

    function formatMaintenanceAssistantAnswer(raw) {
        if (!raw) return "No response.";
        const sections = splitIntoSections(raw);

        const currentCondition = pickFirstNonEmpty(sections, [
            "current condition",
            "condition",
            "status"
        ]);

        const signalHealth = pickFirstNonEmpty(sections, [
            "signal health",
            "signals",
            "telemetry"
        ]);

        const recentActivity = pickFirstNonEmpty(sections, [
            "recent activity",
            "recent events",
            "last 24h",
            "events"
        ]);

        const maintenanceStatus = pickFirstNonEmpty(sections, [
            "maintenance status",
            "maintenance",
            "pm status"
        ]);

        const suggestedChecks = pickFirstNonEmpty(sections, [
            "suggested checks",
            "checks",
            "inspection checks",
            "troubleshooting steps"
        ]);

        const lines = [];
        if (currentCondition) {
            lines.push("CURRENT CONDITION");
            lines.push("  " + currentCondition);
        }
        if (signalHealth) {
            lines.push("\nSIGNAL HEALTH");
            lines.push("  " + signalHealth);
        }
        if (recentActivity) {
            lines.push("\nRECENT ACTIVITY");
            lines.push("  " + recentActivity);
        }
        if (maintenanceStatus) {
            lines.push("\nMAINTENANCE STATUS");
            lines.push("  " + maintenanceStatus);
        }
        if (suggestedChecks) {
            lines.push("\nSUGGESTED CHECKS");
            lines.push("  " + suggestedChecks);
        }

        if (!lines.length) {
            return sections.__all || raw;
        }

        return lines.join("\n");
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
        loadEquipmentList();
    }
})();
