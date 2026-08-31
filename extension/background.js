const api = typeof browser !== "undefined" ? browser : chrome;
const PORT = 48752;
const BASE = `http://127.0.0.1:${PORT}`;

api.runtime.onMessage.addListener(async (msg) => {
  if (msg && msg.type === "ping") {
    try {
      const r = await fetch(`${BASE}/ping`, { cache: "no-store" });
      if (!r.ok) return { ok: false };
      const info = await r.json();
      return { ok: true, info };
    } catch {
      return { ok: false };
    }
  }

  if (msg && msg.type === "install") {
    try {
      const r = await fetch(`${BASE}/install`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          namespace: msg.namespace,
          name: msg.name,
          version: msg.version || ""
        })
      });
      return await r.json();
    } catch (e) {
      return { ok: false, message: "app-offline", detail: String(e) };
    }
  }
});
