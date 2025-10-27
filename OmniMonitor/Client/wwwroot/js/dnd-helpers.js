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

  document.addEventListener('dragstart', function(ev){
    const t = ev.target instanceof Element ? ev.target : null;
    if (!t || !ev.dataTransfer) return;
    const container = t.closest('[data-drag-ghost="card"]');
    if (!container) return;
    try {
      ev.dataTransfer.setDragImage(getTransparentImage(), 0, 0);
    } catch {}
  }, true);
})();
