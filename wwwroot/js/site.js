function showAlert(message) {
    alert(message);
}

// モバイルデバイス判定（画面幅のみで判定）
window.isMobileDevice = function() {
    return window.innerWidth <= 992;
};

// PDFファイルダウンロード機能
window.downloadFile = function(fileName, base64Data) {
    try {
        // Base64データをBlobに変換
        const byteCharacters = atob(base64Data);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
            byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: 'application/pdf' });
        
        // ダウンロードリンクを作成して実行
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
    } catch (error) {
        console.error('ファイルダウンロードエラー:', error);
        alert('ファイルのダウンロードに失敗しました。');
    }
};


// 接続状態チェック
window.isBlazorConnected = function() {
    return window.Blazor && window.Blazor._internal && window.Blazor._internal.navigationManager;
};

// セッションストレージのヘルパー関数
window.setSessionStorage = function(key, value) {
    sessionStorage.setItem(key, value);
};

window.getSessionStorage = function(key) {
    return sessionStorage.getItem(key);
};

window.removeSessionStorage = function(key) {
    sessionStorage.removeItem(key);
};

// モバイル状態変更イベントリスナー
window.setupMobileStateListener = function(dotNetRef) {
    window.addEventListener('mobileStateChanged', function(event) {
        dotNetRef.invokeMethodAsync('OnMobileStateChanged', event.detail.isMobile);
    });
};

// Cookie設定関数
window.setCookie = function(name, value, days) {
    try {
        const expires = new Date();
        expires.setTime(expires.getTime() + (days * 24 * 60 * 60 * 1000));
        const cookieString = `${name}=${value}; expires=${expires.toUTCString()}; path=/; SameSite=Lax`;
        document.cookie = cookieString;
        return true;
    } catch (error) {
        console.error('Cookie設定エラー:', error);
        return false;
    }
};

// Cookie取得関数
window.getCookie = function(name) {
    try {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) {
            const cookieValue = parts.pop().split(';').shift();
            return cookieValue;
        }
        return null;
    } catch (error) {
        console.error('Cookie取得エラー:', error);
        return null;
    }
};
