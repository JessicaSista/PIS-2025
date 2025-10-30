(function(){
  // Use a 1x1 transparent image to completely hide the drag ghost
  let transparentImg = null;
  function getTransparentImage(){
    if (transparentImg) return transparentImg;
    const canvas = document.createElement('canvas');
    canvas.width = 1; canvas.height = 1;
    const ctx = canvas.getContext('2d');
    if (ctx) ctx.clearRect(0,0,1,1);
    const img = new Image();
    img.src = canvas.toDataURL();
    transparentImg = img;
    return img;
  }

  // Keep a reference to the temporary cloned drag image for cleanup
  let currentDragImageEl = null;
  // Track last pointer position to improve offset accuracy on some browsers
  let lastPointer = { x: 0, y: 0 };
  document.addEventListener('pointerdown', function(e){
    if (e && typeof e.clientX === 'number' && typeof e.clientY === 'number') {
      lastPointer.x = e.clientX; lastPointer.y = e.clientY;
    }
  }, true);
  document.addEventListener('mousedown', function(e){
    if (e && typeof e.clientX === 'number' && typeof e.clientY === 'number') {
      lastPointer.x = e.clientX; lastPointer.y = e.clientY;
    }
  }, true);
  document.addEventListener('touchstart', function(e){
    try {
      const t = e.touches && e.touches[0];
      if (t) { lastPointer.x = t.clientX; lastPointer.y = t.clientY; }
    } catch {}
  }, true);

  document.addEventListener('dragstart', function(ev){
    const t = ev.target instanceof Element ? ev.target : null;
    if (!t || !ev.dataTransfer) return;
    const container = t.closest('[data-drag-ghost="card"]');
    if (!container) return;

    // Try to use only the card as the drag image
    const card = container.querySelector('.mud-card');
    if (card) {
      try {
        const rect = card.getBoundingClientRect();
        // Clone the card to use as a drag image
        const clone = card.cloneNode(true);
        const style = clone.style;
        style.position = 'absolute';
        // Place the clone far offscreen to avoid interfering with layout
        style.top = '-10000px';
        style.left = '-10000px';
        style.pointerEvents = 'none';
        style.margin = '0';
        style.boxSizing = 'border-box';
        style.transform = 'none';
        style.display = 'block';
        style.width = rect.width + 'px';
        style.height = rect.height + 'px';
        // Ensure background/border are kept for visibility
        style.background = getComputedStyle(card).background;
        style.border = getComputedStyle(card).border;
        document.body.appendChild(clone);
        currentDragImageEl = clone;

        const clientX = (typeof ev.clientX === 'number' && ev.clientX !== 0) ? ev.clientX : lastPointer.x;
        const clientY = (typeof ev.clientY === 'number' && ev.clientY !== 0) ? ev.clientY : lastPointer.y;
        let offsetX = clientX - rect.left;
        let offsetY = clientY - rect.top;
        // Apply requested adjustment: move ghost 100px left and 300px up
        // This is achieved by increasing the offsets (cursor is further inside the image)
        offsetX += 100; // shift ghost to the left
        offsetY += 300; // shift ghost upwards
        // Clamp offsets to the card bounds
        if (offsetX < 0) offsetX = 0; else if (offsetX > rect.width) offsetX = rect.width;
        if (offsetY < 0) offsetY = 0; else if (offsetY > rect.height) offsetY = rect.height;
        const ua = navigator.userAgent;
        const isFirefox = /Firefox/i.test(ua);
        const dpr = window.devicePixelRatio || 1;
        // Chromium/Edge/WebKit expect CSS pixels; Firefox expects device pixels
        const imgOffsetX = Math.max(0, Math.round(isFirefox ? offsetX * dpr : offsetX));
        const imgOffsetY = Math.max(0, Math.round(isFirefox ? offsetY * dpr : offsetY));
        ev.dataTransfer.setDragImage(clone, imgOffsetX, imgOffsetY);
        return;
      } catch {}
    }

    // Fallback: transparent image (no ghost)
    try {
      ev.dataTransfer.setDragImage(getTransparentImage(), 0, 0);
    } catch {}
  }, true);

  document.addEventListener('dragend', function(){
    if (currentDragImageEl && currentDragImageEl.parentNode) {
      try { currentDragImageEl.parentNode.removeChild(currentDragImageEl); } catch {}
    }
    currentDragImageEl = null;
  }, true);
})();
