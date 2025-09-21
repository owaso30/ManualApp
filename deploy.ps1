# エラー時に停止する設定
$ErrorActionPreference = "Stop"

### ====== 環境変数をあなたの環境に合わせて設定 ======
$SERVER_HOST = "deploy@manualapp.ozarin.net"       # サーバのSSH接続先（ユーザ@ホスト名 or IP）
$APP_DIR = "/var/www/manualapp"             # サーバ側のアプリ配置ルート
$KEEP = 5                                    # 何世代のリリースを残すか（古いものは自動削除）
$RUNTIME = "linux-x64"                       # Self-contained のターゲット
$FRAMEWORK = "net8.0"                        # 使っているターゲットフレームワーク
$PUBLISH_DIR = "./out"                       # ローカルの発行出力先
$HEALTH_URL_LOCAL = "http://127.0.0.1:5000/healthz"   # サーバ側で叩くヘルスチェックURL（/healthz 推奨）
$HEALTH_RETRY = 30                           # ヘルスチェックのリトライ回数
### ================================================

# リリース名（時刻）
$REL = Get-Date -Format "yyyyMMddHHmmss"
Write-Host "==> Build & Deploy release: $REL" -ForegroundColor Green

# 1) ローカルでクリーン発行（Self-contained, 非トリム/非単一ファイル）
Write-Host "==> dotnet publish ..." -ForegroundColor Yellow

# 既存の出力ディレクトリを削除
if (Test-Path $PUBLISH_DIR) {
    Remove-Item -Path $PUBLISH_DIR -Recurse -Force
}

dotnet publish -c Release -f $FRAMEWORK -r $RUNTIME --self-contained true `
  -p:PublishTrimmed=false -p:PublishSingleFile=false `
  -o $PUBLISH_DIR

# 念のため、生成物の実行形式をチェック（ELF ならOK）
try {
    $filePath = Join-Path $PUBLISH_DIR "ManualApp"
    if (Test-Path $filePath) {
        Write-Host "実行ファイルが生成されました: $filePath" -ForegroundColor Cyan
        $fileInfo = Get-Item $filePath
        Write-Host "ファイルサイズ: $($fileInfo.Length) bytes" -ForegroundColor Cyan
    } else {
        Write-Warning "実行ファイルが見つかりません: $filePath"
    }
} catch {
    Write-Warning "ファイルチェック中にエラーが発生しました: $($_.Exception.Message)"
}

# 2) サーバへ転送（所有者/権限はサーバ側で整える）
Write-Host "==> rsync to server ..." -ForegroundColor Yellow
$rsyncArgs = @(
    "-az"
    "--delete"
    "$PUBLISH_DIR/"
    "${SERVER_HOST}:${APP_DIR}/releases/${REL}/"
)
& wsl rsync @rsyncArgs

# 3) サーバ側で：権限 → 実行ビット → メンテON → current切替 → 再起動 → ヘルス → メンテOFF → 古いリリース掃除
Write-Host "==> switching symlink & restart on server ..." -ForegroundColor Yellow

# SSH経由でサーバ側のスクリプトを実行
$serverScript = @"
set -e
set -u
set -o pipefail

REL="$REL"
APP_DIR="$APP_DIR"
KEEP=$KEEP
HEALTH_URL="$HEALTH_URL_LOCAL"
HEALTH_RETRY=$HEALTH_RETRY

cd "`${APP_DIR}"

# (a) 権限・実行権を整える（所有者が deploy なので sudo 不要で通る）
find "releases/`${REL}" -type d -exec chmod 755 {} \;
find "releases/`${REL}" -type f -exec chmod 644 {} \;
# wwwroot内の静的ファイルは読み取り権限を確実に設定
# find "releases/`${REL}/wwwroot" -type f -exec chmod 644 {} \;
[ -f "releases/`${REL}/ManualApp" ] && chmod +x "releases/`${REL}/ManualApp"
mkdir -p "`${APP_DIR}/shared"

# (b) メンテモードON（フラグファイル方式）
touch "`${APP_DIR}/shared/MAINTENANCE_MODE" || true

# (c) current を新リリースへ切替
ln -sfn "releases/`${REL}" current

# (d) systemd 再起動
echo "==> Testing sudo access..."
sudo systemctl status manualapp
echo "==> Restarting service..."
sudo systemctl restart manualapp

# (e) ヘルスチェック
ok=0
for i in `$(seq 1 "`${HEALTH_RETRY}"); do
  if curl -fsS "`${HEALTH_URL}" >/dev/null 2>&1; then ok=1; break; fi
  sleep 1
done
[ "`$ok" -eq 1 ] || echo "!! Health check failed after `${HEALTH_RETRY} s"

# (f) メンテモードOFF
rm -f "`${APP_DIR}/shared/MAINTENANCE_MODE" || true

# (g) 古いリリースを自動削除（KEEP 以外を削除）
# releases/ 下にディレクトリが多数ある前提
# tail -n +`$((KEEP+1)) で KEEP+1 行目以降（古い方）を削除
cd "`${APP_DIR}/releases"
ls -1dt */ 2>/dev/null | tail -n +`$((KEEP+1)) | xargs -r rm -rf

echo "==> Deploy finished on server (release `${REL})"
"@

# SSHでサーバ側スクリプトを実行（WSL経由）
$tempScript = "temp_deploy_script.sh"
# Unix形式の改行でファイルを保存
$serverScript -replace "`r`n", "`n" | Out-File -FilePath $tempScript -Encoding UTF8 -NoNewline
try {
    Get-Content $tempScript -Raw | wsl bash -c "ssh -o StrictHostKeyChecking=accept-new $SERVER_HOST 'bash -s'"
} finally {
    Remove-Item $tempScript -ErrorAction SilentlyContinue
}

Write-Host "✅ 完了: リリース $REL を本番に切り替えました（KEEP=$KEEP)" -ForegroundColor Green
