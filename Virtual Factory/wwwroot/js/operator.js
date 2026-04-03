(function () {
    const equipmentSelect = document.getElementById("equipmentSelect");
    const runStatusEl = document.getElementById("runStatus");
    const alarmStatusEl = document.getElementById("alarmStatus");
    const availabilityEl = document.getElementById("availability1h");
    const orderInfoEl = document.getElementById("orderInfo");
    const orderProgressTextEl = document.getElementById("orderProgressText");
    const orderProgressFillEl = document.getElementById("orderProgressFill");
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
            renderContext(ctx);
        } catch (err) {
            console.error("loadContext failed", err);
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
    }

    async function loadAvailability(equipment) {
        try {
            const res = await fetch(`/api/equipment/${encodeURIComponent(equipment)}/state-availability?hours=1`);
            if (!res.ok) return;
            const data = await res.json();
            if (data && typeof data.runningPercent === "number") {
                availabilityEl.textContent = data.runningPercent.toFixed(1) + "%";
            }
        } catch (err) {
            console.error("loadAvailability failed", err);
        }
    }

    async function askAssistant() {
        if (!currentEquipment) return;
        askButton.disabled = true;
        askButton.textContent = "Thinking...";
        assistantResponseEl.textContent = "Thinking...";
        try {
            const data = await window.AssistantClient.fetchAssistantEquipment(currentEquipment);
            const raw = data.answer ?? data.assistantResponse ?? "No response.";
            assistantResponseEl.textContent = formatOperatorAssistantAnswer(raw);
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
        loadEquipmentList();
    }
})();
