// wwwroot/js/assistant-client.js

async function fetchAssistantEquipment(equipmentId) {
    if (!equipmentId) {
        throw new Error("EquipmentId is required");
    }

    const url = `/api/assistant/equipment/${encodeURIComponent(equipmentId)}`;
    const start = performance.now();

    const response = await fetch(url, {
        method: "POST"
    });

    const duration = Math.round(performance.now() - start);

    console.log(`Assistant equipment call ${url} took ${duration} ms`);

    if (!response.ok) {
        throw new Error(`Assistant equipment request failed: ${response.status}`);
    }

    return await response.json();
}

async function fetchAssistantContext(equipmentId) {
    if (!equipmentId) {
        throw new Error("EquipmentId is required");
    }

    const url = `/api/assistant/context/${encodeURIComponent(equipmentId)}`;

    const response = await fetch(url);

    if (response.status === 404) {
        console.warn(`Assistant context not found for ${equipmentId}`);
        return null;
    }

    if (!response.ok) {
        throw new Error(`Assistant context request failed: ${response.status}`);
    }

    return await response.json();
}

// expose globally for operator.js / maintenance.js / supervisor.js
window.AssistantClient = {
    fetchAssistantEquipment,
    fetchAssistantContext
};
