(() => {
  const api = typeof browser !== "undefined" ? browser : chrome;
  const BTN_ID = "thundercrate-btn";

  // pull namespace/name out of the thunderstore url
  function parsePkg() {
    const host = location.hostname;
    const p = location.pathname.split("/").filter(Boolean);
    if (host === "thunderstore.io") {
      const i = p.indexOf("p");
      if (p[0] === "c" && i >= 0 && p[i + 1] && p[i + 2])
        return { namespace: p[i + 1], name: p[i + 2] };
    }
    if (host.endsWith(".thunderstore.io")) {
      const i = p.indexOf("package");
      if (i >= 0 && p[i + 1] && p[i + 2])
        return { namespace: p[i + 1], name: p[i + 2] };
    }
    return null;
  }

  // an element that actually takes up space on screen
  function isVisible(e) {
    if (!e) return false;
    const r = e.getBoundingClientRect();
    return r.width > 2 && r.height > 2;
  }

  // the VISIBLE green Install button (there's also a hidden mobile one)
  function findRef() {
    const installs = [...document.querySelectorAll(".package-listing-sidebar__install")];
    const vis = installs.find(isVisible);
    if (vis) return vis;
    const els = [...document.querySelectorAll("a,button")].filter(isVisible);
    const txt = (e) => (e.textContent || "").trim().toLowerCase();
    return (
      els.find((e) => txt(e) === "install") ||
      els.find((e) => /install with mod manager/i.test(e.textContent || "")) ||
      els.find((e) => e.matches(".package-listing-sidebar__download")) ||
      els.find((e) => txt(e) === "download") ||
      null
    );
  }

  // replace first text run inside a node
  function swapLabel(node, text) {
    const w = document.createTreeWalker(node, NodeFilter.SHOW_TEXT);
    let t;
    while ((t = w.nextNode())) {
      if ((t.nodeValue || "").trim().length) {
        t.nodeValue = text;
        return true;
      }
    }
    return false;
  }

  function setState(btn, text, cls) {
    if (!swapLabel(btn, text)) btn.textContent = text;
    btn.classList.remove("tc-loading", "tc-done", "tc-fail");
    if (cls) btn.classList.add(cls);
  }

  function toast(text, kind) {
    const t = document.createElement("div");
    t.className = "tc-toast" + (kind ? " tc-toast-" + kind : "");
    t.textContent = text;
    document.body.appendChild(t);
    requestAnimationFrame(() => t.classList.add("tc-show"));
    setTimeout(() => {
      t.classList.remove("tc-show");
      setTimeout(() => t.remove(), 300);
    }, 4200);
  }

  async function onClick(btn) {
    const pkg = parsePkg();
    if (!pkg || btn.dataset.busy === "1") return;
    btn.dataset.busy = "1";
    setState(btn, "Downloading…", "tc-loading");

    const res = await api.runtime.sendMessage({
      type: "install",
      namespace: pkg.namespace,
      name: pkg.name
    });

    btn.dataset.busy = "0";
    if (res && res.ok) {
      setState(btn, "Installed ✓", "tc-done");
      toast(`${pkg.namespace}-${pkg.name} installed to your Mods folder`, "ok");
      setTimeout(() => setState(btn, "Subscribe", null), 4000);
    } else if (res && res.message === "app-offline") {
      setState(btn, "Subscribe", "tc-fail");
      toast("ThunderCrate app isn't running. Open it, then try again.", "fail");
      setTimeout(() => setState(btn, "Subscribe", null), 4000);
    } else {
      setState(btn, "Subscribe", "tc-fail");
      toast("Install failed: " + ((res && res.message) || "unknown error"), "fail");
      setTimeout(() => setState(btn, "Subscribe", null), 4000);
    }
  }

  function buildButton(ref) {
    // clone the green Install button so size + color match exactly
    const btn = ref.cloneNode(true);
    btn.removeAttribute("href");
    btn.removeAttribute("target");
    btn.removeAttribute("download");
    btn.removeAttribute("popovertarget");
    btn.removeAttribute("popovertargetaction");
    setState(btn, "Subscribe", null);
    btn.id = BTN_ID;
    btn.classList.add("tc-btn");
    btn.style.cursor = "pointer";
    btn.style.marginTop = "10px";
    btn.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      onClick(btn);
    });
    return btn;
  }

  function ensure() {
    try {
      if (!parsePkg()) {
        document.getElementById(BTN_ID)?.remove();
        return;
      }
      if (document.getElementById(BTN_ID)) return;

      const ref = findRef();
      if (!ref || !ref.parentElement) return; // wait until the real button exists
      ref.parentElement.insertBefore(buildButton(ref), ref.nextSibling);
    } catch (e) {
      /* keep the heartbeat alive */
    }
  }

  // inject only after hydration settles, so we don't trip react #418
  function start() {
    ensure();
    new MutationObserver(ensure).observe(document.documentElement, {
      childList: true,
      subtree: true
    });
    setInterval(ensure, 700);
  }

  if (document.readyState === "complete") setTimeout(start, 900);
  else window.addEventListener("load", () => setTimeout(start, 900));
})();
