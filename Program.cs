using ManualApp.Components;
using ManualApp.Data;
using ManualApp.Models;
using ManualApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Radzen;
using Microsoft.AspNetCore.Http.Features;
using ManualApp.Security;
using Microsoft.AspNetCore.Authorization;
using DotNetEnv;
using Microsoft.AspNetCore.HttpOverrides;
using QuestPDF.Drawing;
using System.Reflection;
using System.IO;

// .env読み込み
Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

// --------------------
// フォント登録（起動時に一度だけ）
// --------------------
// 注意: この処理はビルド前に実行して、結果をシングルトンとしてDIコンテナに登録します。
var fontRegistration = FontHelper.RegisterFonts(builder.Environment);
builder.Services.AddSingleton(fontRegistration);


// --------------------
// サービス登録
// --------------------

// Razor Components (Interactive Server Components)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true; // ここを追加
    });

// SignalR メッセージサイズ
builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
});

// Razor Pages
builder.Services.AddRazorPages();

// ファイルサイズ制限
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

// Identity
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ManualAppContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

// Google認証設定をOptions Patternで読み込み
builder.Services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection("Authentication:Google"));

// Google認証
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        var googleSettings = builder.Configuration.GetSection("Authentication:Google").Get<GoogleAuthSettings>();
        options.ClientId = googleSettings?.ClientId ?? "";
        options.ClientSecret = googleSettings?.ClientSecret ?? "";
        options.SaveTokens = true;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    });

// // Forwarded Headers
// builder.Services.Configure<ForwardedHeadersOptions>(options =>
// {
//     options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
//     options.KnownNetworks.Clear();
//     options.KnownProxies.Clear();
// });

// AWS設定をOptions Patternで読み込み
builder.Services.Configure<AwsSettings>(builder.Configuration.GetSection("AWS"));

// Radzen, Services, HttpClient, Email など
builder.Services.AddRadzenComponents();
builder.Services.AddScoped<S3Service>();
builder.Services.AddScoped<ManualService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ContentService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailSender, ProductionEmailSender>();
builder.Services.AddScoped<IdentityErrorDescriber, JapaneseIdentityErrorDescriber>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// --------------------
// 認可ポリシー
// --------------------
builder.Services.AddScoped<OwnershipInterceptor>();
builder.Services.AddDbContext<ManualAppContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<OwnershipInterceptor>();
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(interceptor);
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsManualOwner", policy =>
        policy.Requirements.Add(new ManualOwnerRequirement()));
});
builder.Services.AddScoped<IAuthorizationHandler, ManualOwnerHandler>();

// --------------------
// Build App
// --------------------
var app = builder.Build();

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks = { }, // Nginx の IP が固定でないなら空
    KnownProxies = { }
});
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.None,
    Secure = CookieSecurePolicy.Always
});

// 認証・認可
app.UseAuthentication();
app.UseAuthorization();

// ヘルスチェック
app.MapGet("/healthz", () => Results.Ok("OK"));

app.UseAntiforgery();

// Identity UI
app.MapRazorPages();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

app.Run();


// --------------------
// ヘルパークラス
// --------------------
static class FontHelper
{
    // MemoryStream を保持して GC されないようにする
    private static MemoryStream? _regularStream;
    private static MemoryStream? _boldStream;

    public static FontRegistration RegisterFonts(IWebHostEnvironment env)
    {
        var fr = new FontRegistration();

        try
        {
            // UTF8 Provider を確実に登録
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            // 優先: アプリに埋め込んだファイル (ContentRootPath/wwwroot/fonts/*.otf)
            var candidatePaths = new[]
            {
                Path.Combine(env.ContentRootPath, "wwwroot", "fonts", "NotoSansCJKjp-Regular.otf"),
                Path.Combine(env.ContentRootPath, "wwwroot", "fonts", "NotoSansCJKjp-Bold.otf"),
                Path.Combine(env.ContentRootPath, "fonts", "NotoSansCJKjp-Regular.otf"),
                Path.Combine(env.ContentRootPath, "fonts", "NotoSansCJKjp-Bold.otf")
            };

            string? regularPath = null;
            string? boldPath = null;

            if (File.Exists(candidatePaths[0])) regularPath = candidatePaths[0];
            if (File.Exists(candidatePaths[1])) boldPath = candidatePaths[1];

            // システムフォントがあればそちらを優先
            var sysRegular = "/usr/share/fonts/truetype/noto/NotoSansCJK-Regular.ttc";
            var sysOtf = "/usr/share/fonts/opentype/noto/NotoSansCJKjp-Regular.otf";

            if (File.Exists(sysOtf) && regularPath == null)
                regularPath = sysOtf;

            if (File.Exists(sysRegular) && regularPath == null)
                regularPath = sysRegular;

            // Bold のシステム候補
            var sysBold = "/usr/share/fonts/opentype/noto/NotoSansCJKjp-Bold.otf";
            if (File.Exists(sysBold) && boldPath == null)
                boldPath = sysBold;

            // 登録
            if (regularPath != null)
            {
                var bytes = File.ReadAllBytes(regularPath);
                _regularStream = new MemoryStream(bytes);
                _regularStream.Position = 0;
                FontManager.RegisterFont(_regularStream);
                fr.RegularRegistered = true;
                fr.RegularFamily = "Noto Sans CJK JP"; // 通常のシステム名（表示用）
            }

            if (boldPath != null)
            {
                var bytes = File.ReadAllBytes(boldPath);
                _boldStream = new MemoryStream(bytes);
                _boldStream.Position = 0;
                FontManager.RegisterFont(_boldStream);
                fr.BoldRegistered = true;
                fr.BoldFamily = "Noto Sans CJK JP"; // Bold も同じファミリ名で参照する
            }

            // ログ出力
            Console.WriteLine($"Font registration: RegularFound={fr.RegularRegistered}, BoldFound={fr.BoldRegistered}");

            // 追加の注意: システムにフォントが無い場合はユーザーへ案内
            if (!fr.RegularRegistered)
            {
                Console.WriteLine("Warning: Regular font not registered. Please install fonts-noto-cjk on the server (sudo apt install fonts-noto-cjk) or place Noto fonts into wwwroot/fonts/");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Font registration exception: {ex.Message}");
        }

        return fr;
    }
}

public class FontRegistration
{
    public bool RegularRegistered { get; set; } = false;
    public bool BoldRegistered { get; set; } = false;
    public string RegularFamily { get; set; } = "Noto Sans CJK JP";
    public string BoldFamily { get; set; } = "Noto Sans CJK JP";
}

public class GoogleAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}

public class AwsSettings
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
}