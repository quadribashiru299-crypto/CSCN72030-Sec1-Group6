const fields = ["value", "condition", "quality", "time", "outcome"];
async function refresh() {
  try {
    const response = await fetch("/Dashboard/Snapshot", { cache: "no-store" });
    if (!response.ok) throw new Error("Snapshot request failed");
    const snapshot = await response.json();
    document.getElementById("updated").textContent = new Date(snapshot.generatedAtUtc).toISOString();
    const counts = { Normal: 0, Warning: 0, Critical: 0, Unknown: 0 };
    for (const tile of [...snapshot.sensors, ...snapshot.actuators]) {
      const card = [...document.querySelectorAll("[data-device]")].find(el => el.dataset.device === tile.id);
      if (!card) continue;
      if (snapshot.sensors.some(sensor => sensor.id === tile.id)) counts[tile.condition] = (counts[tile.condition] || 0) + 1;
      const values = {
        value: tile.value === null ? "No reading" : `${tile.lastKnown ? "Last known: " : ""}${tile.value}${tile.unit ? " " + tile.unit : ""}`,
        condition: tile.condition,
        quality: tile.quality,
        time: tile.observedAtUtc ? new Date(tile.observedAtUtc).toISOString() : "No confirmed reading",
        outcome: tile.commandOutcome || "None"
      };
      for (const field of fields) {
        const target = card.querySelector(`[data-field="${field}"]`);
        if (target) target.textContent = values[field];
      }
      card.dataset.condition = tile.condition.toLowerCase();
    }
    for (const [condition, count] of Object.entries(counts)) {
      document.querySelector(`[data-count="${condition}"]`).textContent = count;
    }
    const hasReading = [...snapshot.sensors, ...snapshot.actuators].some(tile => tile.observedAtUtc);
    document.querySelector(".notice").textContent = hasReading
      ? "Device snapshots are updating. Lock commands are disabled until authentication, dispatch and audit are connected."
      : "Waiting for the team's acquisition service to publish device readings. Lock commands are disabled until authentication, dispatch and audit are connected.";
  } catch {
    document.querySelector(".notice").textContent = "Dashboard connection lost. Displayed values may be out of date.";
  }
}
setInterval(refresh, 1000);
