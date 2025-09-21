// wwwroot/js/clipboardAnnotator.js
const store = new Map(); // canvasId -> state

function getCtx(canvasId) {
  const canvas = document.getElementById(canvasId);
  if (!canvas) throw new Error(`Canvas '${canvasId}' not found`);
  const ctx = canvas.getContext("2d");
  return { canvas, ctx };
}

function ensureState(canvasId) {
  let st = store.get(canvasId);
  if (!st) {
    st = {
      tool: "arrow",
      color: "#ff3b30",
      width: 3,
      fontPx: 24,
      text: "注記",
      dragging: false,
      startX: 0, startY: 0,
      snapshot: null,
      history: [],
    };
    store.set(canvasId, st);
  }
  return st;
}

function pushHistory(ctx) {
  return ctx.getImageData(0, 0, ctx.canvas.width, ctx.canvas.height);
}

function restoreImageData(ctx, imgData) {
  if (!imgData) return;
  ctx.putImageData(imgData, 0, 0);
}

// 共通の画像リサイズ処理
function resizeImageToMaxSize(img, maxWidth = 1920, maxHeight = 1080) {
  let { width, height } = img;
  
  if (width > maxWidth || height > maxHeight) {
    const ratio = Math.min(maxWidth / width, maxHeight / height);
    width = Math.floor(width * ratio);
    height = Math.floor(height * ratio);
  }
  
  return { width, height };
}

// 共通の画像をDataURLに変換する処理
function imageToDataUrl(img, maxWidth = 1920, maxHeight = 1080, quality = 0.8) {
  const canvas = document.createElement('canvas');
  const ctx = canvas.getContext('2d');
  const { width, height } = resizeImageToMaxSize(img, maxWidth, maxHeight);
  
  canvas.width = width;
  canvas.height = height;
  ctx.drawImage(img, 0, 0, width, height);
  
  return canvas.toDataURL('image/jpeg', quality);
}

window.initCanvas = function initCanvas(canvasId) {
  const { canvas, ctx } = getCtx(canvasId);
  const st = ensureState(canvasId);

  st.history = [pushHistory(ctx)];

  function getPos(ev) {
    const rect = canvas.getBoundingClientRect();
    const x = (ev.clientX ?? ev.touches?.[0]?.clientX) - rect.left;
    const y = (ev.clientY ?? ev.touches?.[0]?.clientY) - rect.top;
    const scaleX = canvas.width / rect.width;
    const scaleY = canvas.height / rect.height;
    return { x: x * scaleX, y: y * scaleY };
  }

  function pointerDown(ev) {
    ev.preventDefault();
    canvas.setPointerCapture?.(ev.pointerId ?? 0);

    const p = getPos(ev);
    st.dragging = true;
    st.startX = p.x; st.startY = p.y;

    if (st.tool === "text") {
      st.history.push(pushHistory(ctx));
      drawText(canvasId, st.text || "テキスト", p.x, p.y, st.fontPx, st.color, true);
      st.dragging = false;
    } else {
      st.snapshot = ctx.getImageData(0, 0, canvas.width, canvas.height);
    }
  }

  function pointerMove(ev) {
    if (!st.dragging || st.tool === "text") return;
    const p = getPos(ev);
    restoreImageData(ctx, st.snapshot);
    
    switch (st.tool) {
      case "arrow": previewArrow(ctx, st.startX, st.startY, p.x, p.y, st.width, st.color); break;
      case "rect": previewRect(ctx, st.startX, st.startY, p.x - st.startX, p.y - st.startY, st.width, st.color); break;
      case "ellipse": {
        const w = p.x - st.startX;
        const h = p.y - st.startY;
        const cx = st.startX + w / 2;
        const cy = st.startY + h / 2;
        const rx = Math.abs(w) / 2;
        const ry = Math.abs(h) / 2;
        previewEllipse(ctx, cx, cy, rx, ry, st.width, st.color); break;
      }
    }
  }

  function pointerUp(ev) {
    if (!st.dragging || st.tool === "text") return;
    st.dragging = false;
    const p = getPos(ev);

    st.history.push(pushHistory(ctx));
    switch (st.tool) {
      case "arrow": drawArrow(canvasId, st.startX, st.startY, p.x, p.y, st.width, st.color); break;
      case "rect": drawRect(canvasId, st.startX, st.startY, p.x - st.startX, p.y - st.startY, st.width, st.color); break;
      case "ellipse": {
        const w = p.x - st.startX;
        const h = p.y - st.startY;
        const cx = st.startX + w / 2;
        const cy = st.startY + h / 2;
        const rx = Math.abs(w) / 2;
        const ry = Math.abs(h) / 2;
        drawEllipse(canvasId, cx, cy, rx, ry, st.width, st.color); break;
      }
    }
    st.snapshot = null;
  }

  canvas.addEventListener("pointerdown", pointerDown);
  canvas.addEventListener("pointermove", pointerMove);
  canvas.addEventListener("pointerup", pointerUp);
  canvas.addEventListener("pointerleave", pointerUp);
}

window.setTool = function setTool(canvasId, tool) {
  ensureState(canvasId).tool = tool;
}

window.setColor = function setColor(canvasId, color) {
  ensureState(canvasId).color = color;
}

window.setWidth = function setWidth(canvasId, w) {
  ensureState(canvasId).width = Number(w) || 3;
}

window.setFontPx = function setFontPx(canvasId, px) {
  ensureState(canvasId).fontPx = Number(px) || 24;
}

window.setText = function setText(canvasId, text) {
  ensureState(canvasId).text = text ?? "";
}

window.pasteFromClipboard = async function pasteFromClipboard(canvasId) {
  const { canvas, ctx } = getCtx(canvasId);
  const st = ensureState(canvasId);
  
  try {
    const items = await navigator.clipboard.read();
    for (const item of items) {
      for (const type of item.types) {
        if (type.startsWith("image/")) {
          const blob = await item.getType(type);
          const bmp = await createImageBitmap(blob);
          const { width, height } = resizeImageToMaxSize(bmp);
          
          canvas.width = width;
          canvas.height = height;
          ctx.clearRect(0, 0, canvas.width, canvas.height);
          ctx.drawImage(bmp, 0, 0, width, height);
          st.history = [ctx.getImageData(0, 0, canvas.width, canvas.height)];
          return true;
        }
      }
    }
    return false;
  } catch (e) {
    console.error("pasteFromClipboard failed:", e);
    throw e;
  }
}

window.drawText = function drawText(canvasId, text, x, y, fontPx = 24, color = "#ff3b30", bold = true) {
  const { ctx } = getCtx(canvasId);
  ctx.save();
  ctx.font = `${bold ? "bold " : ""}${fontPx}px "Meiryo", "Segoe UI", Arial, sans-serif`;
  ctx.fillStyle = color;
  ctx.textBaseline = "top";
  ctx.fillText(text, x, y);
  ctx.restore();
}

function previewArrow(ctx, x1, y1, x2, y2, width, color) {
  drawArrowStroke(ctx, x1, y1, x2, y2, width, color);
}

window.drawArrow = function drawArrow(canvasId, x1, y1, x2, y2, width = 4, color = "#007aff") {
  const { ctx } = getCtx(canvasId);
  drawArrowStroke(ctx, x1, y1, x2, y2, width, color);
}

function drawArrowStroke(ctx, x1, y1, x2, y2, width, color) {
  const headLen = 12;
  const dx = x2 - x1, dy = y2 - y1;
  const angle = Math.atan2(dy, dx);
  ctx.save();
  ctx.lineWidth = width;
  ctx.strokeStyle = color;

  ctx.beginPath();
  ctx.moveTo(x1, y1);
  ctx.lineTo(x2, y2);
  ctx.stroke();

  ctx.beginPath();
  ctx.moveTo(x2, y2);
  ctx.lineTo(x2 - headLen * Math.cos(angle - Math.PI / 6), y2 - headLen * Math.sin(angle - Math.PI / 6));
  ctx.moveTo(x2, y2);
  ctx.lineTo(x2 - headLen * Math.cos(angle + Math.PI / 6), y2 - headLen * Math.sin(angle + Math.PI / 6));
  ctx.stroke();
  ctx.restore();
}

function previewRect(ctx, x, y, w, h, width, color) {
  ctx.save();
  ctx.lineWidth = width;
  ctx.strokeStyle = color;
  ctx.strokeRect(x, y, w, h);
  ctx.restore();
}

window.drawRect = function drawRect(canvasId, x, y, w, h, width = 3, color = "#34c759") {
  const { ctx } = getCtx(canvasId);
  previewRect(ctx, x, y, w, h, width, color);
}

function previewEllipse(ctx, cx, cy, rx, ry, width, color) {
  ctx.save();
  ctx.lineWidth = width;
  ctx.strokeStyle = color;
  ctx.beginPath();
  ctx.ellipse(cx, cy, rx, ry, 0, 0, Math.PI * 2);
  ctx.stroke();
  ctx.restore();
}

window.drawEllipse = function drawEllipse(canvasId, cx, cy, rx, ry, width = 3, color = "#af52de") {
  const { ctx } = getCtx(canvasId);
  previewEllipse(ctx, cx, cy, rx, ry, width, color);
}

window.copyCanvasToClipboard = async function copyCanvasToClipboard(canvasId, type = "image/png", quality) {
  const { canvas } = getCtx(canvasId);
  return new Promise((resolve, reject) => {
    canvas.toBlob(async (blob) => {
      try {
        await navigator.clipboard.write([new ClipboardItem({ [blob.type]: blob })]);
        resolve(true);
      } catch (e) {
        console.error("copyCanvasToClipboard failed:", e);
        reject(e);
      }
    }, type, quality);
  });
}

window.enablePasteShortcut = function enablePasteShortcut(canvasId, elementIdForListener) {
  const el = elementIdForListener ? document.getElementById(elementIdForListener) : document.body;
  el.addEventListener("paste", async (ev) => {
    const { canvas, ctx } = getCtx(canvasId);
    const st = ensureState(canvasId);
    const items = ev.clipboardData?.items || [];
    
    for (const it of items) {
      if (it.type.startsWith("image/")) {
        const file = it.getAsFile();
        if (file) {
          const bmp = await createImageBitmap(file);
          const { width, height } = resizeImageToMaxSize(bmp);
          
          canvas.width = width;
          canvas.height = height;
          ctx.clearRect(0, 0, canvas.width, canvas.height);
          ctx.drawImage(bmp, 0, 0, width, height);
          st.history = [ctx.getImageData(0, 0, canvas.width, canvas.height)];
          break;
        }
      }
    }
  });
}

window.undo = function undo(canvasId) {
  const { ctx } = getCtx(canvasId);
  const st = ensureState(canvasId);
  if (st.history.length <= 1) return false;
  st.history.pop();
  const prev = st.history[st.history.length - 1];
  restoreImageData(ctx, prev);
  return true;
}

window.clearCanvas = function clearCanvas(canvasId) {
  const { canvas, ctx } = getCtx(canvasId);
  const st = ensureState(canvasId);
  ctx.clearRect(0, 0, canvas.width, canvas.height);
  st.history.push(pushHistory(ctx));
}

window.getCanvasAsDataURL = function getCanvasAsDataURL(canvasId, type = "image/png", quality) {
  const { canvas } = getCtx(canvasId);
  return canvas.toDataURL(type, quality);
}

window.copyDataUrlToClipboard = async function copyDataUrlToClipboard(dataUrl) {
  try {
    const response = await fetch(dataUrl);
    const blob = await response.blob();
    await navigator.clipboard.write([new ClipboardItem({ [blob.type]: blob })]);
    return true;
  } catch (error) {
    console.error("クリップボードコピーエラー:", error);
    return false;
  }
}

// 画像要素から直接Blobとしてクリップボードにコピーする関数
window.copyImageElementToClipboard = async function copyImageElementToClipboard(selector) {
  try {
    const img = document.querySelector(selector);
    if (!img) {
      return false;
    }
    
    // 画像が完全に読み込まれているかチェック
    if (!img.complete || img.naturalWidth === 0) {
      return new Promise((resolve) => {
        img.onload = async () => {
          try {
            const result = await copyImageElementToClipboard(selector);
            resolve(result);
          } catch (error) {
            resolve(false);
          }
        };
        img.onerror = () => {
          resolve(false);
        };
      });
    }
    
    // 画像のURLを取得
    const imageUrl = img.src;
    
    // DataURLを使用して画像を取得
    try {
      const dataUrl = await fetchImageAsDataUrl(imageUrl);
      if (!dataUrl) {
        throw new Error("DataURLの取得に失敗しました");
      }
      
      // DataURLをBlobに変換
      const response = await fetch(dataUrl);
      const blob = await response.blob();
      
      // クリップボードAPIの利用可能性をチェック
      if (!navigator.clipboard) {
        throw new Error("クリップボードAPIが利用できません");
      }
      
      await navigator.clipboard.write([new ClipboardItem({ [blob.type]: blob })]);
      return true;
      
    } catch (dataUrlError) {
      // DataURLが失敗した場合は、既存のcopyDataUrlToClipboardを使用
      try {
        const dataUrl = imageToDataUrl(img);
        return await copyDataUrlToClipboard(dataUrl);
      } catch (fallbackError) {
        return false;
      }
    }
    
  } catch (error) {
    return false;
  }
}



// グローバル関数
window.setCursorToEndOfLastLine = function(elementId) {
  const element = document.getElementById(elementId);
  if (element) {
    element.setSelectionRange(element.value.length, element.value.length);
    element.focus();
  }
};

window.preventEnterKey = function(elementId) {
  const element = document.getElementById(elementId);
  if (element && element.value.endsWith('\n')) {
    element.value = element.value.slice(0, -1);
  }
};

// 統合されたクリップボード画像取得関数
window.getClipboardImage = async function getClipboardImage() {
  try {
    const items = await navigator.clipboard.read();
    
    for (const item of items) {
      for (const type of item.types) {
        if (type.startsWith("image/")) {
          const blob = await item.getType(type);
          const img = new Image();
          
          return new Promise((resolve, reject) => {
            const timeout = setTimeout(() => {
              reject(new Error("画像処理がタイムアウトしました"));
            }, 10000);
            
            img.onload = () => {
              clearTimeout(timeout);
              try {
                const dataUrl = imageToDataUrl(img);
                resolve(dataUrl);
              } catch (error) {
                reject(error);
              }
            };
            
            img.onerror = () => {
              clearTimeout(timeout);
              reject(new Error("画像の読み込みに失敗しました"));
            };
            
            img.src = URL.createObjectURL(blob);
          });
        }
      }
    }
    return null;
  } catch (error) {
    console.error("getClipboardImage failed:", error);
    throw error;
  }
}

window.fetchImageAsDataUrl = async function fetchImageAsDataUrl(imageUrl) {
  try {
    const response = await fetch(imageUrl, {
      mode: 'cors',
      credentials: 'omit'
    });
    
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    
    const blob = await response.blob();
    
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result);
      reader.onerror = () => reject(new Error("DataURL変換に失敗しました"));
      reader.readAsDataURL(blob);
    });
  } catch (error) {
    console.error("fetchImageAsDataUrlエラー:", error);
    return null;
  }
}

window.getImageDataUrlFromElement = function getImageDataUrlFromElement(selector) {
  try {
    const img = document.querySelector(selector);
    if (!img) {
      console.error("画像要素が見つかりません:", selector);
      return null;
    }
    
    if (!img.complete || img.naturalWidth === 0) {
      return new Promise((resolve) => {
        img.onload = () => {
          try {
            const dataUrl = imageToDataUrl(img);
            resolve(dataUrl);
          } catch (error) {
            console.error("画像描画エラー:", error);
            resolve(null);
          }
        };
        img.onerror = () => resolve(null);
      });
    }
    
    return imageToDataUrl(img);
  } catch (error) {
    console.error("画像DataURL取得エラー:", error);
    return null;
  }
}

window.loadImageFromDataUrl = async function loadImageFromDataUrl(canvasId, dataUrl) {
  const { canvas, ctx } = getCtx(canvasId);
  const st = ensureState(canvasId);
  
  return new Promise((resolve, reject) => {
    const img = new Image();
    
    img.onload = () => {
      try {
        const { width, height } = resizeImageToMaxSize(img);
        canvas.width = width;
        canvas.height = height;
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.drawImage(img, 0, 0, width, height);
        st.history = [ctx.getImageData(0, 0, canvas.width, canvas.height)];
        resolve(true);
      } catch (error) {
        reject(new Error(`画像の描画に失敗しました: ${error.message}`));
      }
    };
    
    img.onerror = () => {
      reject(new Error("DataURL画像の読み込みに失敗しました"));
    };
    
    img.src = dataUrl;
  });
}

window.loadImageFromUrl = async function loadImageFromUrl(canvasId, imageUrl) {
  const { canvas, ctx } = getCtx(canvasId);
  const st = ensureState(canvasId);
  
  return new Promise((resolve, reject) => {
    const img = new Image();
    let corsAttempted = false;
    
    const timeout = setTimeout(() => {
      reject(new Error("画像の読み込みがタイムアウトしました"));
    }, 10000);
    
    const loadImage = () => {
      img.onload = () => {
        clearTimeout(timeout);
        try {
          const { width, height } = resizeImageToMaxSize(img);
          canvas.width = width;
          canvas.height = height;
          ctx.clearRect(0, 0, canvas.width, canvas.height);
          ctx.drawImage(img, 0, 0, width, height);
          st.history = [ctx.getImageData(0, 0, canvas.width, canvas.height)];
          resolve("success");
        } catch (error) {
          reject(new Error(`画像の描画に失敗しました: ${error.message}`));
        }
      };
      
      img.onerror = () => {
        clearTimeout(timeout);
        
        if (!imageUrl.startsWith('data:') && !corsAttempted) {
          corsAttempted = true;
          img.crossOrigin = null;
          img.src = imageUrl;
          return;
        }
        
        reject(new Error(`画像の読み込みに失敗しました。URL: ${imageUrl.substring(0, 100)}...`));
      };
      
      try {
        img.src = imageUrl;
      } catch (error) {
        clearTimeout(timeout);
        reject(new Error(`無効な画像URLです: ${error.message}`));
      }
    };
    
    if (!imageUrl.startsWith('data:')) {
      img.crossOrigin = "anonymous";
      corsAttempted = true;
    }
    
    loadImage();
  });
}