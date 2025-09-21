using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using System.Text;
using ManualApp.Models;
using System.Net.Http;

namespace ManualApp.Services;

public interface IPdfService
{
    Task<byte[]> GenerateManualPdfAsync(ManualApp.Models.Manual manual, List<ManualApp.Models.Content> contents);
}

public class PdfService : IPdfService
{
    private readonly HttpClient _httpClient;
    private readonly FontRegistration _fontRegistration;

    public PdfService(HttpClient httpClient, FontRegistration fontRegistration)
    {
        _httpClient = httpClient;
        _fontRegistration = fontRegistration;
    }

    public async Task<byte[]> GenerateManualPdfAsync(ManualApp.Models.Manual manual, List<ManualApp.Models.Content> contents)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // キャッシュ設定やデバッグは必要に応じて
        QuestPDF.Settings.EnableCaching = false;
        QuestPDF.Settings.EnableDebugging = true;

        // 文字エンコーディング
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // フォントは Program.cs で一度だけ登録済みである想定
        var regularFamily = _fontRegistration.RegularRegistered ? _fontRegistration.RegularFamily : "MS Gothic";
        var boldFamily = _fontRegistration.BoldRegistered ? _fontRegistration.BoldFamily : regularFamily;

        // 画像を事前にダウンロード
        var imageCache = await DownloadImagesAsync(contents);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x
                    .FontFamily(regularFamily)
                    .FontSize(12));

                // ヘッダー
                page.Header()
                    .Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text(manual.Title ?? "マニュアル")
                                .FontSize(18)
                                .FontColor(Colors.Blue.Medium)
                                .FontFamily(boldFamily)
                                .Bold();

                            column.Item().Text($"作成日: {DateTime.Now:yyyy年MM月dd日}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Medium);
                        });
                    });

                // フッター
                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("ページ ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });

                // コンテンツ
                page.Content()
                    .PaddingVertical(0.4f, Unit.Centimetre)
                    .Column(column =>
                    {
                        for (int i = 0; i < contents.Count; i += 4)
                        {
                            var pageContents = contents.Skip(i).Take(4).ToList();

                            // 上段
                            column.Item().Row(row =>
                            {
                                RenderContentCard(row, pageContents, 0, imageCache, boldFamily);
                                RenderContentCard(row, pageContents, 1, imageCache, boldFamily);
                            });

                            // 下段
                            if (pageContents.Count > 2)
                            {
                                column.Item().Row(row =>
                                {
                                    RenderContentCard(row, pageContents, 2, imageCache, boldFamily);
                                    RenderContentCard(row, pageContents, 3, imageCache, boldFamily);
                                });
                            }

                            // 改ページ
                            if (i + 4 < contents.Count)
                            {
                                column.Item().PageBreak();
                            }
                        }
                    });
            });
        });

        return document.GeneratePdf();
    }

    private async Task<Dictionary<string, byte[]>> DownloadImagesAsync(List<ManualApp.Models.Content> contents)
    {
        var imageCache = new Dictionary<string, byte[]>();

        foreach (var content in contents.Where(c => c.Image != null))
        {
            try
            {
                var imageBytes = await _httpClient.GetByteArrayAsync(content.Image!.FilePath);
                imageCache[content.Image.FilePath] = imageBytes;
            }
            catch
            {
                // 読み込み失敗は無視
            }
        }

        return imageCache;
    }

    private void RenderContentCard(RowDescriptor row, List<ManualApp.Models.Content> pageContents, int index, Dictionary<string, byte[]> imageCache, string boldFamily)
    {
        if (pageContents.Count > index)
        {
            var content = pageContents[index];
            row.RelativeItem(0.5f)
                .Padding(5)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(Colors.Grey.Lighten5)
                .Column(column =>
                {
                    // 手順番号
                    column.Item()
                        .PaddingBottom(5)
                        .Text($"手順 {content.Order}")
                        .FontSize(14)
                        .FontFamily(boldFamily)
                        .Bold()
                        .FontColor(Colors.Blue.Medium);

                    // 画像
                    column.Item()
                        .Height(220)
                        .Background(Colors.Grey.Lighten4)
                        .AlignCenter()
                        .AlignMiddle()
                        .Element(element =>
                        {
                            if (content.Image != null && imageCache.ContainsKey(content.Image.FilePath))
                            {
                                element.Image(imageCache[content.Image.FilePath])
                                    .WithRasterDpi(400)
                                    .FitArea();
                            }
                            else if (content.Image != null)
                            {
                                element.Text("📷 画像読み込みエラー")
                                    .FontSize(10)
                                    .FontColor(Colors.Red.Medium);
                            }
                            else
                            {
                                element.Text("📷 画像なし")
                                    .FontSize(10)
                                    .FontColor(Colors.Grey.Medium);
                            }
                        });

                    // テキスト
                    column.Item()
                        .Height(110)
                        .PaddingTop(5)
                        .PaddingHorizontal(10)
                        .Text(content.Text ?? "コンテンツがありません")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken2);
                });
        }
        else
        {
            row.RelativeItem(0.5f); // 空スペース
        }
    }
}
