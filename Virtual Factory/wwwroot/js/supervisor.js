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

            const worst = data
                .slice()
                .sort((a, b) => (a.runningPercent || 0) - (b.runningPercent || 0))
                .slice(0, 5);

            if (!worst.length) {
                worstTableBody.innerHTML = "<tr><td colspan=\"2\">No data</td></tr>";
            } else {
                worstTableBody.innerHTML = worst.map(x =>
                    `<tr data-eq="${escapeHtml(x.equipmentId)}">
                        <td>${escapeHtml(x.equipmentId)}</td>
                        <td>${(x.runningPercent || 0).toFixed(1)}%</td>
                    </tr>`
                ).join("");

                // clicking a row drills down
                worstTableBody.querySelectorAll("tr").forEach(tr => {
                    tr.addEventListener("click", () => {
                        const eq = tr.getAttribute("data-eq");
                        if (eq) {
                            setDrilldownEquipment(eq);
                        }
                    });
                });
            }
        } catch (err) {
            console.error("loadFleetAvailability failed", err);
            worstTableBody.innerHTML = "<tr><td colspan=\"2\">Failed to load</td></tr>";
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

    async function setDrilldownEquipment(eq) {
        if (!eq) return;
        currentEquipment = eq;
        if (drilldownSelect) {
            drilldownSelect.value = eq;
        }
        await refreshDrilldown();
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
            }
        } catch (err) {
            console.error("loadDrilldownAvailability failed", err);
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
            assistantResponseEl.textContent = formatSupervisorAssistantAnswer(raw);
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

    // initial load
    loadFleetAvailability();
    loadEquipmentList();
})();
