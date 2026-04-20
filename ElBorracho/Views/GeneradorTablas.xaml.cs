using ElBorracho.Services;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ElBorracho.Views;

public partial class GeneradorTablasPage : ContentPage
{
    private readonly CartaRepository _repo;
    private int _cantidad = 4;

    public GeneradorTablasPage(CartaRepository repo)
    {
        InitializeComponent();
        _repo = repo;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    protected override bool OnBackButtonPressed()
    {
        _ = Shell.Current.GoToAsync("..");
        return true;
    }

    private void OnDecrementar(object? sender, EventArgs e)
    {
        if (_cantidad > 1) _cantidad--;
        LblCantidad.Text = _cantidad.ToString();
    }

    private void OnIncrementar(object? sender, EventArgs e)
    {
        if (_cantidad < 20) _cantidad++;
        LblCantidad.Text = _cantidad.ToString();
    }

    private async void OnGenerarClicked(object? sender, EventArgs e)
    {
        try
        {
            var rng = new Random();
            var tables = new List<List<ElBorracho.Models.Carta>>();

            for (int t = 0; t < _cantidad; t++)
            {
                var cartas = _repo.FreshDeck()
                                  .OrderBy(_ => rng.Next())
                                  .Take(16)
                                  .ToList();
                tables.Add(cartas);
            }

            // Build PDF
            var doc = new PdfDocument();
            foreach (var table in tables)
            {
                var page = doc.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                page.Orientation = PdfSharpCore.PageOrientation.Portrait;
                var gfx = XGraphics.FromPdfPage(page);

                var margin = 40.0;
                var pageW = page.Width.Point;
                var pageH = page.Height.Point;
                var usableW = pageW - margin * 2;

                XFont titleFont;
                XFont cellFont;
                XFont numFont;

                try
                {
                    titleFont = new XFont("OpenSans", 20, XFontStyle.Bold);
                    cellFont  = new XFont("OpenSans", 11, XFontStyle.Regular);
                    numFont   = new XFont("OpenSans", 9,  XFontStyle.Regular);
                }
                catch
                {
                    titleFont = new XFont("Arial", 20, XFontStyle.Bold);
                    cellFont  = new XFont("Arial", 11, XFontStyle.Regular);
                    numFont   = new XFont("Arial", 9,  XFontStyle.Regular);
                }

                // Title
                gfx.DrawString("🎴 Tabla de Lotería — El Borracho",
                    titleFont, XBrushes.DarkRed,
                    new XRect(margin, 18, usableW, 32), XStringFormats.TopCenter);

                // Decorative line
                gfx.DrawLine(new XPen(XColor.FromArgb(212, 23, 90), 2),
                    margin, 56, pageW - margin, 56);

                // 4×4 grid
                var gridTop   = 68.0;
                var cellW     = usableW / 4.0;
                var cellH     = cellW * 0.65;

                for (int r = 0; r < 4; r++)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        var idx   = r * 4 + c;
                        var carta = table[idx];
                        var x     = margin + c * cellW;
                        var y     = gridTop + r * cellH;

                        // Cell background (alternating)
                        var bgColor = (r + c) % 2 == 0
                            ? XColor.FromArgb(250, 240, 220)
                            : XColor.FromArgb(255, 250, 240);
                        gfx.DrawRectangle(new XSolidBrush(bgColor), x, y, cellW - 3, cellH - 3);
                        gfx.DrawRectangle(new XPen(XColor.FromArgb(36, 18, 8), 1.2), x, y, cellW - 3, cellH - 3);

                        // Card number (top-left)
                        gfx.DrawString($"{carta.Numero}",
                            numFont, new XSolidBrush(XColor.FromArgb(160, 120, 80)),
                            new XRect(x + 4, y + 3, 20, 12), XStringFormats.TopLeft);

                        // Card name (centered)
                        gfx.DrawString(carta.Nombre,
                            cellFont, XBrushes.Black,
                            new XRect(x + 4, y + 4, cellW - 11, cellH - 11),
                            XStringFormats.Center);
                    }
                }

                // Footer
                var footerY = gridTop + 4 * cellH + 8;
                gfx.DrawString($"Lotería El Borracho  ·  {DateTime.Now:dd/MM/yyyy}",
                    numFont, new XSolidBrush(XColor.FromArgb(160, 120, 136)),
                    new XRect(margin, footerY, usableW, 16), XStringFormats.TopCenter);
            }

            // Save to cache directory
            var fileName = $"tablas_loteria_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            using (var ms = new MemoryStream())
            {
                doc.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                using var fs = File.Create(filePath);
                ms.CopyTo(fs);
            }

            // Share the PDF
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"Tablas de Lotería ({_cantidad})",
                File  = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error al generar PDF",
                $"Ocurrió un problema:\n{ex.Message}", "OK");
        }
    }
}