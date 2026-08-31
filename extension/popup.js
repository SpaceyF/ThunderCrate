const api = typeof browser !== "undefined" ? browser : chrome;
const THEMES = ["storm", "catppuccin"];

// apply saved theme before paint (script sits in <head>)
(function applyTheme() {
  let t = "storm";
  try { t = localStorage.getItem("tc-theme") || "storm"; } catch {}
  if (!THEMES.includes(t)) t = "storm";
  document.documentElement.dataset.theme = t;
})();

function selectTheme(t) {
  document.documentElement.dataset.theme = t;
  try { localStorage.setItem("tc-theme", t); } catch {}
  document.querySelectorAll("[data-theme-btn]").forEach((b) => {
    b.classList.toggle("active", b.dataset.themeBtn === t);
  });
}

async function refreshStatus() {
  const path = document.getElementById("path");
  const res = await api.runtime.sendMessage({ type: "ping" });
  if (res && res.ok && res.info && res.info.mods) {
    path.textContent = res.info.mods;
  } else if (res && res.ok) {
    path.textContent = "";
  } else {
    path.textContent = "the app isn't open right now";
  }
  path.hidden = false;
}

document.addEventListener("DOMContentLoaded", () => {
  selectTheme(document.documentElement.dataset.theme || "storm");
  document.getElementById("seg").addEventListener("click", (e) => {
    const btn = e.target.closest("[data-theme-btn]");
    if (btn) selectTheme(btn.dataset.themeBtn);
  });
  refreshStatus();
});
