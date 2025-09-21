function showAlert(message) {
    alert(message);
}

// モバイルデバイス判定
window.isMobileDevice = function() {
    return window.innerWidth <= 768 || /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
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
