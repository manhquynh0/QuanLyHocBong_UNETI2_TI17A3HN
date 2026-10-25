(() => {
  const toast = document.querySelector("[data-scholarship-toast]");
  if (!toast) {
    return;
  }

  const dismissButton = toast.querySelector(".scholarship-toast-close");
  let dismissTimer;

  const dismiss = () => {
    window.clearTimeout(dismissTimer);
    toast.classList.remove("is-visible");
    toast.classList.add("is-leaving");
    window.setTimeout(() => toast.remove(), 220);
  };

  dismissButton?.addEventListener("click", dismiss);

  window.requestAnimationFrame(() => {
    toast.classList.add("is-visible");
    dismissTimer = window.setTimeout(dismiss, 5700);
  });
})();
