using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace WinWidget;

public sealed partial class WidgetWindow : Window
{
    private readonly bool demo;
    private readonly StackPanel body = new();
    private readonly TextBlock status = Label("Ready", 11, "#858B97");
    private readonly Forms.NotifyIcon tray;
    private readonly Button pin;
    private readonly string statePath;
    private bool loading = true;

    public WidgetWindow(bool demo)
    {
        this.demo = demo;
        statePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinWidget", "window.json");
        Title = "AI Usage · WinWidget";
        Width = 360; Height = 640;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true; Background = Brushes.Transparent;
        Topmost = true; ShowInTaskbar = false;
        FontFamily = new FontFamily("Segoe UI");
        Left = Math.Max(0, SystemParameters.WorkArea.Right - Width - 24);
        Top = SystemParameters.WorkArea.Top + 40;
        LoadPosition();
        var shell = new Border { Background = Brush("#12151C"), BorderBrush = Brush("#313743"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(18), Padding = new Thickness(20) };
        Content = shell;
        var layout = new DockPanel(); shell.Child = layout;
        var header = new Grid { Margin = new Thickness(0, 0, 0, 22), Background = Brushes.Transparent };
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new StackPanel();
        title.Children.Add(Label("AI USAGE", 12, "#F1F3F7", FontWeights.SemiBold));
        title.Children.Add(Label(demo ? "Desktop companion · demo" : "Desktop companion", 11, "#858B97"));
        title.MouseLeftButtonDown += (_, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
        header.Children.Add(title);
        pin = Button("Pinned", (_, _) => { Topmost = !Topmost; pin!.Content = Topmost ? "Pinned" : "Pin"; SavePosition(); });
        Grid.SetColumn(pin, 1); header.Children.Add(pin);
        DockPanel.SetDock(header, Dock.Top); layout.Children.Add(header);
        var footer = new StackPanel();
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 10) };
        actions.Children.Add(Button("Refresh", async (_, _) => await RefreshAsync()));
        actions.Children.Add(Button("Settings", (_, _) => OpenSettings()));
        actions.Children.Add(Button("Hide", (_, _) => Hide()));
        footer.Children.Add(actions); footer.Children.Add(status);
        DockPanel.SetDock(footer, Dock.Bottom); layout.Children.Add(footer);
        layout.Children.Add(body);
        tray = new Forms.NotifyIcon { Icon = System.Drawing.SystemIcons.Application, Text = "AI Usage · WinWidget", Visible = !demo };
        tray.DoubleClick += (_, _) => { Show(); Activate(); };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Show widget", null, (_, _) => { Show(); Activate(); });
        menu.Items.Add("Settings", null, (_, _) => OpenSettings());
        menu.Items.Add("Quit", null, (_, _) => Close()); tray.ContextMenuStrip = menu;
        LocationChanged += (_, _) => { if (!loading) SavePosition(); };
        Closed += (_, _) => { SavePosition(); tray.Dispose(); StopRefresh(); };
        loading = false;
        RenderInitial();
        Loaded += async (_, _) => { await RefreshAsync(); StartRefresh(); };
    }

    private void RenderInitial()
    {
        body.Children.Clear();
        body.Children.Add(Card("DEEPSEEK", demo ? "$24.80" : "—", "API balance · USD", "#74B4FF"));
        body.Children.Add(Card("MINIMAX", demo ? "$18.60" : "—", "API credit · USD", "#C5A2FF"));
        body.Children.Add(Card("CODEX", demo ? "82%" : "—", "Subscription quota", "#86DFC0"));
        body.Children.Add(Label("REMAINING HISTORY", 10, "#858B97", FontWeights.SemiBold));
        body.Children.Add(Label("Snapshots will appear after refresh.", 12, "#A4ACBA"));
    }

    internal static SolidColorBrush Brush(string value) => new((Color)ColorConverter.ConvertFromString(value));
    internal static TextBlock Label(string value, double size = 13, string color = "#F1F3F7", FontWeight? weight = null) => new()
    { Text = value, FontSize = size, Foreground = Brush(color), FontWeight = weight ?? FontWeights.Normal, Margin = new Thickness(0, 0, 0, 5), TextWrapping = TextWrapping.Wrap };
    internal static Button Button(string text, RoutedEventHandler action)
    {
        var button = new Button { Content = text, Foreground = Brush("#DCE1EA"), Background = Brush("#232833"), BorderThickness = new Thickness(0), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 7, 0), Cursor = Cursors.Hand };
        button.Click += action; return button;
    }
    private static Border Card(string title, string value, string note, string accent)
    {
        var stack = new StackPanel();
        stack.Children.Add(Label(title, 10, accent, FontWeights.SemiBold));
        stack.Children.Add(Label(value, 30, "#F1F3F7", FontWeights.SemiBold));
        stack.Children.Add(Label(note, 11, "#A4ACBA"));
        return new Border { Child = stack, Background = Brush("#1B2029"), CornerRadius = new CornerRadius(12), Padding = new Thickness(16), Margin = new Thickness(0, 0, 0, 10) };
    }
    private void LoadPosition()
    {
        if (demo) return;
        try
        {
            if (!File.Exists(statePath)) return;
            var state = JsonSerializer.Deserialize<WindowStateData>(File.ReadAllText(statePath));
            if (state == null || !double.IsFinite(state.Left) || !double.IsFinite(state.Top)) return;
            // Clamp to the primary work area so a removed monitor cannot strand the widget.
            Left = Math.Clamp(state.Left, SystemParameters.WorkArea.Left, Math.Max(SystemParameters.WorkArea.Left, SystemParameters.WorkArea.Right - Width));
            Top = Math.Clamp(state.Top, SystemParameters.WorkArea.Top, Math.Max(SystemParameters.WorkArea.Top, SystemParameters.WorkArea.Bottom - Height));
            Topmost = state.Pinned;
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { }
    }
    private void SavePosition()
    {
        if (demo) return;
        try { Directory.CreateDirectory(Path.GetDirectoryName(statePath)!); File.WriteAllText(statePath, JsonSerializer.Serialize(new WindowStateData(Left, Top, Topmost))); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { status.Text = "Window position could not be saved."; }
    }
    private record WindowStateData(double Left, double Top, bool Pinned);
    private System.Threading.Tasks.Task RefreshAsync() { status.Text = demo ? "Demo · sample values" : "Configure providers in Settings"; return System.Threading.Tasks.Task.CompletedTask; }
    private void OpenSettings() => MessageBox.Show(this, "Provider settings arrive in iteration 2.", "WinWidget");
    private void StartRefresh() { }
    private void StopRefresh() { }
}
