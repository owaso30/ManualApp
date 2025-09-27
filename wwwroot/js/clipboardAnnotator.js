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
      tool: "select",
      color: "#ff3b30",
      width: 7,
      fontPx: 24,
      text: "注記",
      dragging: false,
      startX: 0, startY: 0,
      snapshot: null,
      history: [],
      objects: [], // 描画されたオブジェクトのリスト
      selectedObject: null, // 選択されたオブジェクト
      isMovingObject: false, // オブジェクト移動中かどうか
      moveStartX: 0, moveStartY: 0, // 移動開始位置
      objectHistory: [], // オブジェクト操作の履歴
      isResizingArrow: false, // 矢印のリサイズ中かどうか
      resizeHandle: null, // リサイズ中のハンドル（'start' または 'end'）
      isResizingText: false, // テキストの文字サイズ変更中かどうか
    };
    store.set(canvasId, st);
  }
  return st;
}

function pushHistory(ctx) {
  return ctx.getImageData(0, 0, ctx.canvas.width, ctx.canvas.height);
}

// オブジェクト履歴を保存
function pushObjectHistory(canvasId, action, objectData) {
  const st = ensureState(canvasId);
  st.objectHistory.push({
    action: action, // 'add', 'move', 'delete'
    object: objectData,
    timestamp: Date.now()
  });
  
  // 履歴が多くなりすぎないように制限
  if (st.objectHistory.length > 50) {
    st.objectHistory.shift();
  }
}

// オブジェクト履歴から最後の操作を取得
function popObjectHistory(canvasId) {
  const st = ensureState(canvasId);
  return st.objectHistory.pop();
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
  
  // デフォルトで選択ツールに設定
  st.tool = "select";
  
  // 初期カーソルを設定
  setCursor(canvasId, st.tool);

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
    
    if (st.tool === "select") {
      // 選択ツールの場合
      if (st.selectedObject && st.selectedObject.type === "arrow") {
        // 矢印が選択されている場合、ハンドルをチェック
        const handle = isPointInArrowHandle(p.x, p.y, st.selectedObject);
        if (handle) {
          st.isResizingArrow = true;
          st.resizeHandle = handle;
          st.moveStartX = p.x;
          st.moveStartY = p.y;
          return;
        }
      }
      
      if (st.selectedObject && st.selectedObject.type === "text") {
        // テキストが選択されている場合、文字サイズ変更ハンドルをチェック
        if (isPointInTextSizeHandle(p.x, p.y, st.selectedObject)) {
          st.isResizingText = true;
          st.moveStartX = p.x;
          st.moveStartY = p.y;
          return;
        }
      }
      
      const selectedObj = selectObject(canvasId, p.x, p.y);
      if (selectedObj) {
        st.isMovingObject = true;
        st.moveStartX = p.x;
        st.moveStartY = p.y;
      }
    } else {
      // 描画ツールの場合
      st.dragging = true;
      st.startX = p.x; st.startY = p.y;

      if (st.tool === "text") {
        st.history.push(pushHistory(ctx));
        drawText(canvasId, st.text || "テキスト", p.x, p.y, st.fontPx, st.color, true);
        st.dragging = false;
        // テキスト配置後に選択ツールに戻る
        st.tool = "select";
        setCursor(canvasId, "select");
      } else {
        st.snapshot = ctx.getImageData(0, 0, canvas.width, canvas.height);
      }
    }
  }

  function pointerMove(ev) {
    const p = getPos(ev);
    
    if (st.tool === "select" && st.isResizingArrow && st.selectedObject) {
      // 矢印のリサイズ中
      const dx = p.x - st.moveStartX;
      const dy = p.y - st.moveStartY;
      resizeArrow(canvasId, dx, dy);
      st.moveStartX = p.x;
      st.moveStartY = p.y;
    } else if (st.tool === "select" && st.isResizingText && st.selectedObject) {
      // テキストの文字サイズ変更中
      const dx = p.x - st.moveStartX;
      const dy = p.y - st.moveStartY;
      resizeText(canvasId, dx, dy);
      st.moveStartX = p.x;
      st.moveStartY = p.y;
    } else if (st.tool === "select" && st.isMovingObject && st.selectedObject) {
      // オブジェクト移動中
      const dx = p.x - st.moveStartX;
      const dy = p.y - st.moveStartY;
      moveObject(canvasId, dx, dy);
      st.moveStartX = p.x;
      st.moveStartY = p.y;
    } else if (st.dragging && st.tool !== "text") {
      // 描画プレビュー中
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
  }

  function pointerUp(ev) {
    if (st.tool === "select") {
      // 選択ツールの場合
      if (st.isResizingArrow) {
        finishResizeArrow(canvasId);
        st.isResizingArrow = false;
        st.resizeHandle = null;
      } else if (st.isResizingText) {
        finishResizeText(canvasId);
        st.isResizingText = false;
      } else if (st.isMovingObject) {
        finishMoveObject(canvasId);
        st.isMovingObject = false;
      }
    } else if (st.dragging && st.tool !== "text") {
      // 描画ツールの場合
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
      
      // オブジェクト配置後に選択ツールに戻る
      st.tool = "select";
      setCursor(canvasId, "select");
    }
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
  ensureState(canvasId).width = Number(w) || 7;
}

window.setFontPx = function setFontPx(canvasId, px) {
  ensureState(canvasId).fontPx = Number(px) || 24;
}

window.setCursor = function setCursor(canvasId, tool) {
  const { canvas } = getCtx(canvasId);
  
  switch (tool) {
    case "select":
      canvas.style.cursor = "default";
      break;
    case "text":
      canvas.style.cursor = "text";
      break;
    case "arrow":
    case "rect":
    case "ellipse":
      canvas.style.cursor = "crosshair";
      break;
    default:
      canvas.style.cursor = "default";
  }
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
  const st = ensureState(canvasId);
  
  ctx.save();
  ctx.font = `${bold ? "bold " : ""}${fontPx}px "Meiryo", "Segoe UI", Arial, sans-serif`;
  ctx.fillStyle = color;
  ctx.textBaseline = "top";
  ctx.fillText(text, x, y);
  
  // テキストの境界を正確に計算
  const metrics = ctx.measureText(text);
  const textWidth = metrics.width;
  const textHeight = fontPx;
  
  ctx.restore();
  
  // オブジェクト情報を保存
  const obj = {
    id: Date.now() + Math.random(),
    type: "text",
    x: x,
    y: y,
    width: textWidth,
    height: textHeight,
    text: text,
    fontPx: fontPx,
    originalFontPx: fontPx, // 元の文字サイズを保存
    color: color,
    bold: bold
  };
  st.objects.push(obj);
  
  // オブジェクト履歴に追加
  pushObjectHistory(canvasId, 'add', obj);
}

function previewArrow(ctx, x1, y1, x2, y2, width, color) {
  drawArrowStroke(ctx, x1, y1, x2, y2, width, color);
}

window.drawArrow = function drawArrow(canvasId, x1, y1, x2, y2, width = 7, color = "#007aff") {
  const { ctx } = getCtx(canvasId);
  const st = ensureState(canvasId);
  
  drawArrowStroke(ctx, x1, y1, x2, y2, width, color);
  
  // オブジェクト情報を保存
  const obj = {
    id: Date.now() + Math.random(),
    type: "arrow",
    x1: x1,
    y1: y1,
    x2: x2,
    y2: y2,
    strokeWidth: width, // widthプロパティ名を変更
    color: color,
    // 境界ボックスを計算（矢印の線の範囲）
    x: Math.min(x1, x2),
    y: Math.min(y1, y2),
    width: Math.abs(x2 - x1),
    height: Math.abs(y2 - y1)
  };
  st.objects.push(obj);
  
  // オブジェクト履歴に追加
  pushObjectHistory(canvasId, 'add', obj);
}

function drawArrowStroke(ctx, x1, y1, x2, y2, width, color) {
  const headLen = 12;
  const dx = x2 - x1, dy = y2 - y1;
  const angle = Math.atan2(dy, dx);
  ctx.save();
  ctx.lineWidth = width;
  ctx.strokeStyle = color;
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';

  // 矢印の線を描画
  ctx.beginPath();
  ctx.moveTo(x1, y1);
  ctx.lineTo(x2, y2);
  ctx.stroke();

  // 矢印の先端を描画
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

window.drawRect = function drawRect(canvasId, x, y, w, h, width = 7, color = "#34c759") {
  const { ctx } = getCtx(canvasId);
  const st = ensureState(canvasId);
  
  previewRect(ctx, x, y, w, h, width, color);
  
  // オブジェクト情報を保存
  const obj = {
    id: Date.now() + Math.random(),
    type: "rect",
    x: x,
    y: y,
    width: Math.abs(w),
    height: Math.abs(h),
    strokeWidth: width,
    color: color
  };
  st.objects.push(obj);
  
  // オブジェクト履歴に追加
  pushObjectHistory(canvasId, 'add', obj);
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

window.drawEllipse = function drawEllipse(canvasId, cx, cy, rx, ry, width = 7, color = "#af52de") {
  const { ctx } = getCtx(canvasId);
  const st = ensureState(canvasId);
  
  previewEllipse(ctx, cx, cy, rx, ry, width, color);
  
  // オブジェクト情報を保存
  const obj = {
    id: Date.now() + Math.random(),
    type: "ellipse",
    cx: cx,
    cy: cy,
    rx: rx,
    ry: ry,
    x: cx - rx,
    y: cy - ry,
    width: rx * 2,
    height: ry * 2,
    strokeWidth: width,
    color: color
  };
  st.objects.push(obj);
  
  // オブジェクト履歴に追加
  pushObjectHistory(canvasId, 'add', obj);
}

window.copyCanvasToClipboard = async function copyCanvasToClipboard(canvasId, type = "image/png", quality) {
  const { canvas, ctx } = getCtx(canvasId);
  const st = ensureState(canvasId);
  
  // 一時的に選択枠を非表示にしてコピー
  const wasSelected = st.selectedObject;
  st.selectedObject = null;
  
  // 選択枠なしで再描画
  redrawCanvas(canvasId, false);
  
  return new Promise((resolve, reject) => {
    canvas.toBlob(async (blob) => {
      try {
        await navigator.clipboard.write([new ClipboardItem({ [blob.type]: blob })]);
        
        // 選択状態を復元
        st.selectedObject = wasSelected;
        
        // 選択枠ありで再描画
        if (wasSelected) {
          redrawCanvas(canvasId, true);
        }
        
        resolve(true);
      } catch (e) {
        console.error("copyCanvasToClipboard failed:", e);
        
        // エラー時も選択状態を復元
        st.selectedObject = wasSelected;
        if (wasSelected) {
          redrawCanvas(canvasId, true);
        }
        
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
  const st = ensureState(canvasId);
  
  // オブジェクト履歴から最後の操作を取得
  const lastAction = popObjectHistory(canvasId);
  if (!lastAction) return false;
  
  switch (lastAction.action) {
    case 'add':
      // オブジェクトを削除
      const addIndex = st.objects.findIndex(obj => obj.id === lastAction.object.id);
      if (addIndex !== -1) {
        st.objects.splice(addIndex, 1);
      }
      break;
      
    case 'move':
      // オブジェクトを元の位置に戻す
      const moveObj = st.objects.find(obj => obj.id === lastAction.object.id);
      if (moveObj) {
        moveObj.x = lastAction.object.x;
        moveObj.y = lastAction.object.y;
        if (moveObj.type === 'arrow') {
          moveObj.x1 = lastAction.object.x1;
          moveObj.y1 = lastAction.object.y1;
          moveObj.x2 = lastAction.object.x2;
          moveObj.y2 = lastAction.object.y2;
          // 境界ボックスも更新
          moveObj.width = Math.abs(moveObj.x2 - moveObj.x1);
          moveObj.height = Math.abs(moveObj.y2 - moveObj.y1);
        } else if (moveObj.type === 'ellipse') {
          moveObj.cx = lastAction.object.cx;
          moveObj.cy = lastAction.object.cy;
        }
      }
      break;
      
    case 'resize':
      // オブジェクトを元のサイズに戻す
      const resizeObj = st.objects.find(obj => obj.id === lastAction.object.id);
      if (resizeObj) {
        if (resizeObj.type === 'arrow') {
          resizeObj.x1 = lastAction.object.x1;
          resizeObj.y1 = lastAction.object.y1;
          resizeObj.x2 = lastAction.object.x2;
          resizeObj.y2 = lastAction.object.y2;
          // 境界ボックスも更新
          resizeObj.x = Math.min(resizeObj.x1, resizeObj.x2);
          resizeObj.y = Math.min(resizeObj.y1, resizeObj.y2);
          resizeObj.width = Math.abs(resizeObj.x2 - resizeObj.x1);
          resizeObj.height = Math.abs(resizeObj.y2 - resizeObj.y1);
        } else if (resizeObj.type === 'text') {
          // テキストの文字サイズを元に戻す
          resizeObj.fontPx = lastAction.object.fontPx;
          // テキストのサイズを再計算
          const { ctx } = getCtx(canvasId);
          ctx.save();
          ctx.font = `${resizeObj.bold ? "bold " : ""}${resizeObj.fontPx}px "Meiryo", "Segoe UI", Arial, sans-serif`;
          const textWidth = ctx.measureText(resizeObj.text).width;
          const textHeight = resizeObj.fontPx;
          ctx.restore();
          resizeObj.width = textWidth;
          resizeObj.height = textHeight;
        }
      }
      break;
  }
  
  // キャンバスを再描画（選択枠は表示しない）
  redrawCanvas(canvasId, false);
  return true;
}

window.clearCanvas = function clearCanvas(canvasId) {
  const { canvas, ctx } = getCtx(canvasId);
  const st = ensureState(canvasId);
  
  // オブジェクトと選択状態をクリア
  st.objects = [];
  st.selectedObject = null;
  
  // オブジェクト履歴もクリア（全消去は元に戻せない操作）
  st.objectHistory = [];
  
  // 背景画像（最初の履歴）を復元
  if (st.history.length > 0) {
    restoreImageData(ctx, st.history[0]);
  } else {
    // 履歴がない場合はキャンバスをクリア
    ctx.clearRect(0, 0, canvas.width, canvas.height);
  }
}

window.getCanvasAsDataURL = function getCanvasAsDataURL(canvasId, type = "image/png", quality) {
  const { canvas, ctx } = getCtx(canvasId);
  const st = ensureState(canvasId);
  
  // 一時的に選択枠を非表示にして保存
  const wasSelected = st.selectedObject;
  st.selectedObject = null;
  
  // 選択枠なしで再描画
  redrawCanvas(canvasId, false);
  
  // 画像データを取得
  const dataURL = canvas.toDataURL(type, quality);
  
  // 選択状態を復元
  st.selectedObject = wasSelected;
  
  // 選択枠ありで再描画
  if (wasSelected) {
    redrawCanvas(canvasId, true);
  }
  
  return dataURL;
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

// オブジェクト選択機能
window.selectObject = function selectObject(canvasId, x, y) {
  const st = ensureState(canvasId);
  
  // 後ろから順にチェック（最後に描画されたオブジェクトが優先）
  for (let i = st.objects.length - 1; i >= 0; i--) {
    const obj = st.objects[i];
    if (isPointInObject(x, y, obj)) {
      st.selectedObject = obj;
      redrawCanvas(canvasId, true); // 選択枠を表示
      return obj;
    }
  }
  
  st.selectedObject = null;
  redrawCanvas(canvasId, false); // 選択枠を非表示
  return null;
}

// ポイントがオブジェクト内にあるかチェック
function isPointInObject(x, y, obj) {
  switch (obj.type) {
    case "text":
      // テキストの幅を再計算して正確な判定を行う
      const canvas = document.getElementById("annotCanvas");
      if (canvas) {
        const ctx = canvas.getContext("2d");
        ctx.save();
        ctx.font = `${obj.bold ? "bold " : ""}${obj.fontPx}px "Meiryo", "Segoe UI", Arial, sans-serif`;
        const metrics = ctx.measureText(obj.text);
        const textWidth = metrics.width;
        ctx.restore();
        
        return x >= obj.x && x <= obj.x + textWidth && y >= obj.y && y <= obj.y + obj.height;
      }
      return x >= obj.x && x <= obj.x + obj.width && y >= obj.y && y <= obj.y + obj.height;
    case "arrow":
      // 矢印の場合は線の近くにあるかチェック
      return isPointNearLine(x, y, obj.x1, obj.y1, obj.x2, obj.y2, 10);
    case "rect":
      return x >= obj.x && x <= obj.x + obj.width && y >= obj.y && y <= obj.y + obj.height;
    case "ellipse":
      const dx = x - obj.cx;
      const dy = y - obj.cy;
      return (dx * dx) / (obj.rx * obj.rx) + (dy * dy) / (obj.ry * obj.ry) <= 1;
    default:
      return false;
  }
}

// 矢印のハンドルがクリックされたかチェック
function isPointInArrowHandle(x, y, obj) {
  if (obj.type !== "arrow") return null;
  
  const handleSize = 8;
  const threshold = handleSize / 2 + 2; // 少し余裕を持たせる
  
  // 開始点のハンドル
  const distToStart = Math.sqrt((x - obj.x1) * (x - obj.x1) + (y - obj.y1) * (y - obj.y1));
  if (distToStart <= threshold) {
    return 'start';
  }
  
  // 終了点のハンドル
  const distToEnd = Math.sqrt((x - obj.x2) * (x - obj.x2) + (y - obj.y2) * (y - obj.y2));
  if (distToEnd <= threshold) {
    return 'end';
  }
  
  return null;
}

// テキストの文字サイズ変更ハンドルがクリックされたかチェック
function isPointInTextSizeHandle(x, y, obj) {
  if (obj.type !== "text") return false;
  
  const handleSize = 8;
  const padding = 4;
  const handleX = obj.x + obj.width + padding - handleSize/2;
  const handleY = obj.y + obj.height + padding - handleSize/2;
  
  return x >= handleX && x <= handleX + handleSize && 
         y >= handleY && y <= handleY + handleSize;
}

// ポイントが線の近くにあるかチェック
function isPointNearLine(px, py, x1, y1, x2, y2, threshold) {
  const A = px - x1;
  const B = py - y1;
  const C = x2 - x1;
  const D = y2 - y1;
  
  const dot = A * C + B * D;
  const lenSq = C * C + D * D;
  
  if (lenSq === 0) return Math.sqrt(A * A + B * B) <= threshold;
  
  let param = dot / lenSq;
  
  let xx, yy;
  if (param < 0) {
    xx = x1;
    yy = y1;
  } else if (param > 1) {
    xx = x2;
    yy = y2;
  } else {
    xx = x1 + param * C;
    yy = y1 + param * D;
  }
  
  const dx = px - xx;
  const dy = py - yy;
  return Math.sqrt(dx * dx + dy * dy) <= threshold;
}

// キャンバス全体を再描画
function redrawCanvas(canvasId, showSelectionBox = true) {
  const { canvas, ctx } = getCtx(canvasId);
  const st = ensureState(canvasId);
  
  // 履歴から背景を復元
  if (st.history.length > 0) {
    restoreImageData(ctx, st.history[0]);
  }
  
  // すべてのオブジェクトを再描画
  st.objects.forEach(obj => {
    drawObject(ctx, obj);
  });
  
  // 選択されたオブジェクトの枠を描画（showSelectionBoxがtrueの場合のみ）
  if (showSelectionBox && st.selectedObject) {
    drawSelectionBox(ctx, st.selectedObject);
  }
}

// オブジェクトを描画
function drawObject(ctx, obj) {
  ctx.save();
  
  switch (obj.type) {
    case "text":
      ctx.font = `${obj.bold ? "bold " : ""}${obj.fontPx}px "Meiryo", "Segoe UI", Arial, sans-serif`;
      ctx.fillStyle = obj.color;
      ctx.textBaseline = "top";
      ctx.fillText(obj.text, obj.x, obj.y);
      
      // テキストの幅を再計算して更新
      const metrics = ctx.measureText(obj.text);
      obj.width = metrics.width;
      break;
    case "arrow":
      // 矢印の線幅はstrokeWidthプロパティを使用
      const arrowWidth = obj.strokeWidth || obj.width || 4;
      drawArrowStroke(ctx, obj.x1, obj.y1, obj.x2, obj.y2, arrowWidth, obj.color);
      break;
    case "rect":
      ctx.lineWidth = obj.strokeWidth;
      ctx.strokeStyle = obj.color;
      ctx.strokeRect(obj.x, obj.y, obj.width, obj.height);
      break;
    case "ellipse":
      ctx.lineWidth = obj.strokeWidth;
      ctx.strokeStyle = obj.color;
      ctx.beginPath();
      ctx.ellipse(obj.cx, obj.cy, obj.rx, obj.ry, 0, 0, Math.PI * 2);
      ctx.stroke();
      break;
  }
  
  ctx.restore();
}

// 選択枠を描画
function drawSelectionBox(ctx, obj) {
  ctx.save();
  ctx.strokeStyle = "#007aff";
  ctx.lineWidth = 2;
  ctx.setLineDash([5, 5]);
  
  if (obj.type === "arrow") {
    // 矢印の場合は両端にハンドルを描画
    const padding = 8;
    const handleSize = 8;
    
    // 矢印の線を描画
    ctx.setLineDash([5, 5]);
    ctx.beginPath();
    ctx.moveTo(obj.x1, obj.y1);
    ctx.lineTo(obj.x2, obj.y2);
    ctx.stroke();
    
    // 両端のハンドルを描画
    ctx.setLineDash([]);
    ctx.fillStyle = "#007aff";
    ctx.fillRect(obj.x1 - handleSize/2, obj.y1 - handleSize/2, handleSize, handleSize);
    ctx.fillRect(obj.x2 - handleSize/2, obj.y2 - handleSize/2, handleSize, handleSize);
    
    // ハンドルの境界線
    ctx.strokeStyle = "#ffffff";
    ctx.lineWidth = 1;
    ctx.strokeRect(obj.x1 - handleSize/2, obj.y1 - handleSize/2, handleSize, handleSize);
    ctx.strokeRect(obj.x2 - handleSize/2, obj.y2 - handleSize/2, handleSize, handleSize);
  } else if (obj.type === "text") {
    // テキストの場合は選択枠と文字サイズ変更ハンドル
    const padding = 4;
    ctx.strokeRect(
      obj.x - padding,
      obj.y - padding,
      obj.width + padding * 2,
      obj.height + padding * 2
    );
    
    // 文字サイズ変更ハンドル（右下）
    const handleSize = 8;
    const handleX = obj.x + obj.width + padding - handleSize/2;
    const handleY = obj.y + obj.height + padding - handleSize/2;
    
    ctx.setLineDash([]);
    ctx.fillStyle = "#007aff";
    ctx.fillRect(handleX, handleY, handleSize, handleSize);
    
    // ハンドルの境界線
    ctx.strokeStyle = "#ffffff";
    ctx.lineWidth = 1;
    ctx.strokeRect(handleX, handleY, handleSize, handleSize);
  } else {
    // その他のオブジェクトは通常の選択枠
    const padding = 4;
    ctx.strokeRect(
      obj.x - padding,
      obj.y - padding,
      obj.width + padding * 2,
      obj.height + padding * 2
    );
  }
  
  ctx.restore();
}

// オブジェクト移動機能
window.moveObject = function moveObject(canvasId, dx, dy) {
  const st = ensureState(canvasId);
  
  if (!st.selectedObject) return;
  
  const obj = st.selectedObject;
  
  // 移動前の状態を保存（初回移動時のみ）
  if (!obj.moveStartPos) {
    obj.moveStartPos = {
      x: obj.x,
      y: obj.y,
      x1: obj.x1,
      y1: obj.y1,
      x2: obj.x2,
      y2: obj.y2,
      cx: obj.cx,
      cy: obj.cy
    };
  }
  
  switch (obj.type) {
    case "text":
      obj.x += dx;
      obj.y += dy;
      break;
    case "arrow":
      obj.x1 += dx;
      obj.y1 += dy;
      obj.x2 += dx;
      obj.y2 += dy;
      obj.x += dx;
      obj.y += dy;
      break;
    case "rect":
      obj.x += dx;
      obj.y += dy;
      break;
    case "ellipse":
      obj.cx += dx;
      obj.cy += dy;
      obj.x += dx;
      obj.y += dy;
      break;
  }
  
  redrawCanvas(canvasId, true); // 移動中は選択枠を表示
}

// オブジェクト移動完了時の処理
window.finishMoveObject = function finishMoveObject(canvasId) {
  const st = ensureState(canvasId);
  
  if (!st.selectedObject || !st.selectedObject.moveStartPos) return;
  
  const obj = st.selectedObject;
  
  // 移動が発生した場合のみ履歴に追加
  const hasMoved = obj.moveStartPos.x !== obj.x || obj.moveStartPos.y !== obj.y;
  
  if (hasMoved) {
    // 移動前の状態を履歴に保存
    const moveHistory = {
      id: obj.id,
      type: obj.type,
      ...obj.moveStartPos
    };
    pushObjectHistory(canvasId, 'move', moveHistory);
  }
  
  // 移動開始位置をクリア
  delete obj.moveStartPos;
}

// 矢印のリサイズ機能
window.resizeArrow = function resizeArrow(canvasId, dx, dy) {
  const st = ensureState(canvasId);
  
  if (!st.selectedObject || st.selectedObject.type !== "arrow" || !st.resizeHandle) return;
  
  const obj = st.selectedObject;
  
  // リサイズ前の状態を保存（初回リサイズ時のみ）
  if (!obj.resizeStartPos) {
    obj.resizeStartPos = {
      x1: obj.x1,
      y1: obj.y1,
      x2: obj.x2,
      y2: obj.y2
    };
  }
  
  if (st.resizeHandle === 'start') {
    obj.x1 += dx;
    obj.y1 += dy;
  } else if (st.resizeHandle === 'end') {
    obj.x2 += dx;
    obj.y2 += dy;
  }
  
  // 境界ボックスを更新
  obj.x = Math.min(obj.x1, obj.x2);
  obj.y = Math.min(obj.y1, obj.y2);
  obj.width = Math.abs(obj.x2 - obj.x1);
  obj.height = Math.abs(obj.y2 - obj.y1);
  
  redrawCanvas(canvasId, true);
}

// 矢印リサイズ完了時の処理
window.finishResizeArrow = function finishResizeArrow(canvasId) {
  const st = ensureState(canvasId);
  
  if (!st.selectedObject || !st.selectedObject.resizeStartPos) return;
  
  const obj = st.selectedObject;
  
  // リサイズが発生した場合のみ履歴に追加
  const hasResized = obj.resizeStartPos.x1 !== obj.x1 || obj.resizeStartPos.y1 !== obj.y1 ||
                     obj.resizeStartPos.x2 !== obj.x2 || obj.resizeStartPos.y2 !== obj.y2;
  
  if (hasResized) {
    // リサイズ前の状態を履歴に保存
    const resizeHistory = {
      id: obj.id,
      type: obj.type,
      ...obj.resizeStartPos
    };
    pushObjectHistory(canvasId, 'resize', resizeHistory);
  }
  
  // リサイズ開始位置をクリア
  delete obj.resizeStartPos;
}

// テキストの文字サイズ変更
window.resizeText = function resizeText(canvasId, dx, dy) {
  const st = ensureState(canvasId);
  
  if (!st.selectedObject || st.selectedObject.type !== "text") return;
  
  const obj = st.selectedObject;
  
  // 文字サイズを変更（dxの値に基づいて調整）
  const sizeChange = Math.round(dx / 2); // 感度調整
  const newSize = Math.max(8, Math.min(200, obj.fontPx + sizeChange));
  
  if (newSize !== obj.fontPx) {
    obj.fontPx = newSize;
    
    // テキストのサイズを再計算
    const { ctx } = getCtx(canvasId);
    ctx.save();
    ctx.font = `${obj.fontPx}px "Noto Sans CJK JP", sans-serif`;
    const textWidth = ctx.measureText(obj.text).width;
    const textHeight = obj.fontPx;
    ctx.restore();
    
    // 境界ボックスを更新
    obj.width = textWidth;
    obj.height = textHeight;
    
    redrawCanvas(canvasId, true);
  }
}

// テキストの文字サイズ変更完了時の処理
window.finishResizeText = function finishResizeText(canvasId) {
  const st = ensureState(canvasId);
  
  if (!st.selectedObject || st.selectedObject.type !== "text") return;
  
  const obj = st.selectedObject;
  
  // 文字サイズ変更が発生した場合のみ履歴に追加
  if (obj.fontPx !== obj.originalFontPx) {
    // 変更前の状態を履歴に保存
    const resizeHistory = {
      id: obj.id,
      type: obj.type,
      fontPx: obj.originalFontPx || 24
    };
    pushObjectHistory(canvasId, 'resize', resizeHistory);
  }
}

// ESCキーのイベントハンドラー
document.addEventListener("keydown", (e) => {
  if (e.key === "Escape") {
    const canvas = document.getElementById("annotCanvas");
    if (canvas) {
      // イベントの伝播を停止してダイアログが閉じないようにする
      e.preventDefault();
      e.stopPropagation();
      
      const st = ensureState("annotCanvas");
      st.tool = "select";
      st.dragging = false;
      st.snapshot = null;
      st.selectedObject = null;
      st.isMovingObject = false;
      st.isResizingArrow = false;
      st.resizeHandle = null;
      st.isResizingText = false;
      redrawCanvas("annotCanvas", false);
      setCursor("annotCanvas", "select");
      
      // 選択ツールに切り替えたことをUIに通知
      if (window.setTool) {
        window.setTool("select");
      }
    }
  }
}, true); // capture phaseでイベントをキャッチ