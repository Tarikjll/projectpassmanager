(() => {
  const storageKey = "passmanager-theme";
  const root = document.documentElement;
  const toggles = document.querySelectorAll("[data-theme-toggle]");
  const canUsePremiumTheme = root.getAttribute("data-premium-theme") === "true";

  if (!canUsePremiumTheme) {
    root.setAttribute("data-theme", "light");
    localStorage.removeItem(storageKey);
    return;
  }

  function applyTheme(theme) {
    root.setAttribute("data-theme", theme);
    localStorage.setItem(storageKey, theme);

    toggles.forEach((toggle) => {
      const label = toggle.querySelector("[data-theme-toggle-label]");
      if (label) {
        label.textContent = theme === "dark" ? "Dark" : "Light";
      }
    });
  }

  const initialTheme = root.getAttribute("data-theme") || localStorage.getItem(storageKey) || "light";
  applyTheme(initialTheme);

  toggles.forEach((toggle) => {
    toggle.addEventListener("click", () => {
      applyTheme(root.getAttribute("data-theme") === "dark" ? "light" : "dark");
    });
  });
})();
