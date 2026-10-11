using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WinWidget;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        bool demo = args.Contains("--demo") || args.Contains("--smoke");
        using var instance = new Mutex(true, "Local\\OpenAICompatibleWinWidget" + (demo ? "Demo" : ""), out bool first);
        if (!first) return;
        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        var window = new WidgetWindow(demo);
        if (args.Contains("--smoke"))
        {
            window.ContentRendered += (_, _) => window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(window);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                var output = args.FirstOrDefault(a => a.StartsWith("--output="))?[9..] ?? "widget-preview.png";
                using (var stream = File.Create(output)) encoder.Save(stream);
                window.Close();
            }));
        }
        app.Run(window);
    }
}
