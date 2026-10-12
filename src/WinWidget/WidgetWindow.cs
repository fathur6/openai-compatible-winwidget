using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Forms = System.Windows.Forms;
using WinWidget.Core;

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
    private readonly LocalStore store = new();
    private readonly ProviderClient client = new();
    private Preferences preferences;
    private Snapshot snapshot;
    private bool refreshing;
    private readonly System.Threading.CancellationTokenSource cancellation = new();
    private readonly System.Windows.Threading.DispatcherTimer refreshTimer = new() { Interval = TimeSpan.FromMinutes(15) };
    private readonly System.Windows.Threading.DispatcherTimer minuteTimer = new() { Interval = TimeSpan.FromMinutes(1) };
    private HistoryStore? history;
    private System.Collections.Generic.IReadOnlyList<HistoryPoint> historyPoints = Array.Empty<HistoryPoint>();
    private DateTimeOffset chartEnd = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 3600 * 3600);
    private int chartDayOffset;

    public WidgetWindow(bool demo)
    {
        this.demo = demo;
        preferences = demo ? new() : store.LoadPreferences();
        snapshot = demo ? Snapshot.Demo : store.LoadCache();
        try { if (!demo) history = new HistoryStore(store.Root); }
        catch (Exception e) when (e is SqliteStoreException or IOException or UnauthorizedAccessException or DllNotFoundException or EntryPointNotFoundException) { status.Text = "History storage unavailable."; }
        LoadHistory();
        statePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinWidget", "window.json");
        Title = "AI Usage · WinWidget";
        Width = 368; Height = Math.Min(748, SystemParameters.WorkArea.Height - 24);
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true; Background = Brushes.Transparent;
        Topmost = true; ShowInTaskbar = false;
        FontFamily = new FontFamily("Segoe UI");
        Left = Math.Max(0, SystemParameters.WorkArea.Right - Width - 24);
        Top = Math.Min(SystemParameters.WorkArea.Top + 40, SystemParameters.WorkArea.Bottom - Height);
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
        pin = Button(Topmost ? "Pinned" : "Pin", (_, _) => { Topmost = !Topmost; pin!.Content = Topmost ? "Pinned" : "Pin"; SavePosition(); });
        Grid.SetColumn(pin, 1); header.Children.Add(pin);
        DockPanel.SetDock(header, Dock.Top); layout.Children.Add(header);
        var footer = new StackPanel();
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 10) };
        actions.Children.Add(Button("Refresh", async (_, _) => await RefreshAsync()));
        actions.Children.Add(Button("Settings", (_, _) => OpenSettings()));
        actions.Children.Add(Button("Hide", (_, _) => Hide()));
        footer.Children.Add(actions); footer.Children.Add(status);
        DockPanel.SetDock(footer, Dock.Bottom); layout.Children.Add(footer);
        layout.Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        tray = new Forms.NotifyIcon { Icon = System.Drawing.SystemIcons.Application, Text = "AI Usage · WinWidget", Visible = !demo };
        tray.DoubleClick += (_, _) => { Show(); Activate(); };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Show widget", null, (_, _) => { Show(); Activate(); });
        menu.Items.Add("Settings", null, (_, _) => OpenSettings());
        menu.Items.Add("Quit", null, (_, _) => Close()); tray.ContextMenuStrip = menu;
        LocationChanged += (_, _) => { if (!loading) SavePosition(); };
        Closed += (_, _) => { SavePosition(); tray.Dispose(); StopRefresh(); };
        loading = false;
        RenderSnapshot();
        Loaded += async (_, _) => { await RefreshAsync(); StartRefresh(); };
    }

    private void RenderSnapshot()
    {
        body.Children.Clear();
        if (preferences.ShowDeepSeek) body.Children.Add(BalanceCard("DEEPSEEK", snapshot.DeepSeek, "#74B4FF"));
        if (preferences.ShowMiniMax) body.Children.Add(BalanceCard("MINIMAX", snapshot.MiniMax, "#C5A2FF"));
        if (preferences.ShowCodex)
        {
            var quota = new StackPanel();
            quota.Children.Add(Label("CODEX SUBSCRIPTION", 10, "#86DFC0", FontWeights.SemiBold));
            quota.Children.Add(QuotaRow("5 hour", snapshot.Codex.FiveHour));
            quota.Children.Add(QuotaRow("Weekly", snapshot.Codex.Weekly));
            if (snapshot.Codex.Error != null) quota.Children.Add(Label(snapshot.Codex.Error, 10, "#DEBC84"));
            body.Children.Add(Panel(quota));
        }
        RenderHistory();
    }

    private static Border BalanceCard(string name, ProviderResult result, string accent)
    {
        var stack = new StackPanel();
        stack.Children.Add(Label(name, 10, accent, FontWeights.SemiBold));
        stack.Children.Add(Label(result.Reading == null ? "—" : result.Reading.Value.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-US")), 28, "#F1F3F7", FontWeights.SemiBold));
        stack.Children.Add(Label(result.Error ?? "Available balance · USD", 11, result.Error == null ? "#A4ACBA" : "#DEBC84"));
        if (result.Reading != null) stack.Children.Add(Label($"{(result.Error != null ? "Cached · " : "Updated · ")}{result.Reading.ObservedAt.ToLocalTime():MMM d, h:mm tt}", 10, "#858B97"));
        return Panel(stack);
    }
    private static Border Panel(UIElement content) => new() { Child = content, Background = Brush("#1B2029"), CornerRadius = new CornerRadius(12), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 10) };
    private static StackPanel QuotaRow(string name, Reading? reading)
    {
        var row = new StackPanel { Margin = new Thickness(0, 3, 0, 2) };
        row.Children.Add(Label($"{name}    {(reading == null ? "—" : $"{reading.Value:0}% remaining")}", 13, "#E1E6EE", FontWeights.SemiBold));
        row.Children.Add(new ProgressBar { Value = reading?.Value ?? 0, Maximum = 100, Height = 5, Background = Brush("#353D48"), Foreground = Brush("#86DFC0"), BorderThickness = new Thickness(0), Margin = new Thickness(0, 3, 0, 7) });
        var text = reading?.ResetAt is DateTimeOffset reset ? $"Resets {reset.ToLocalTime():MMM d, h:mm tt}" : "Reset time unavailable";
        if (reading?.ResetAt <= DateTimeOffset.UtcNow) text = "Reset time passed · refresh pending";
        row.Children.Add(Label(text, 10, "#A4ACBA"));
        if (reading != null) row.Children.Add(Label($"Observed {reading.ObservedAt.ToLocalTime():MMM d, h:mm tt}", 9, "#858B97"));
        return row;
    }

    internal static SolidColorBrush Brush(string value) => new((Color)ColorConverter.ConvertFromString(value));
    internal static TextBlock Label(string value, double size = 13, string color = "#F1F3F7", FontWeight? weight = null) => new()
    { Text = value, FontSize = size, Foreground = Brush(color), FontWeight = weight ?? FontWeights.Normal, Margin = new Thickness(0, 0, 0, 2), TextWrapping = TextWrapping.Wrap };
    internal static Button Button(string text, RoutedEventHandler action)
    {
        var button = new Button { Content = text, Foreground = Brush("#DCE1EA"), Background = Brush("#232833"), BorderThickness = new Thickness(0), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 7, 0), Cursor = Cursors.Hand };
        button.Click += action; return button;
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
    private async System.Threading.Tasks.Task RefreshAsync()
    {
        if (refreshing) return;
        if (demo) { status.Text = "Demo · sample values · no network requests"; return; }
        refreshing = true; status.Text = "Refreshing providers…";
        try
        {
            snapshot = await client.RefreshAsync(preferences, store.LoadSecrets(), snapshot, cancellation.Token);
            store.SaveCache(snapshot);
            bool historySaved = SaveHistory();
            RenderSnapshot();
            status.Text = historySaved ? $"Checked {DateTime.Now:h:mm tt} · auto-refresh 15 min" : "Providers checked · history storage unavailable";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { status.Text = "Local data could not be saved."; RenderSnapshot(); }
        finally { refreshing = false; }
    }
    private void OpenSettings()
    {
        if (demo) { MessageBox.Show(this, "Demo mode uses sample data. Launch normally to configure providers.", "WinWidget"); return; }
        var settings = new SettingsWindow(store) { Owner = this };
        if (settings.ShowDialog() == true) { preferences = store.LoadPreferences(); RenderSnapshot(); _ = RefreshAsync(); }
    }
    private void StartRefresh()
    {
        if (demo) return;
        refreshTimer.Tick += async (_, _) => await RefreshAsync(); refreshTimer.Start();
        minuteTimer.Tick += (_, _) => RenderSnapshot(); minuteTimer.Start();
    }
    private void StopRefresh() { refreshTimer.Stop(); minuteTimer.Stop(); cancellation.Cancel(); client.Dispose(); }
    private void RenderHistory()
    {
        var heading = new StackPanel { Orientation = Orientation.Horizontal };
        heading.Children.Add(Label("REMAINING · 7 DAYS", 10, "#858B97", FontWeights.SemiBold));
        var older = Button("‹", (_, _) => { if (chartDayOffset > -358) { chartDayOffset--; LoadHistory(); RenderSnapshot(); } }); older.Padding = new Thickness(8, 0, 8, 0); older.Margin = new Thickness(12, 0, 4, 0); heading.Children.Add(older);
        var newer = Button("›", (_, _) => { if (chartDayOffset < 0) { chartDayOffset++; LoadHistory(); RenderSnapshot(); } }); newer.Padding = new Thickness(8, 0, 8, 0); newer.IsEnabled = chartDayOffset < 0; heading.Children.Add(newer);
        body.Children.Add(heading);
        body.Children.Add(new HistoryChart { Points = historyPoints, End = chartEnd, ShowDeepSeek = preferences.ShowDeepSeek, ShowCodex = preferences.ShowCodex });
        var legend = new StackPanel { Orientation = Orientation.Horizontal };
        if (preferences.ShowDeepSeek) legend.Children.Add(Label("DeepSeek USD   ", 10, "#74B4FF"));
        if (preferences.ShowCodex) legend.Children.Add(Label("Codex weekly %", 10, "#86DFC0"));
        body.Children.Add(legend);
    }
    private void LoadHistory()
    {
        chartEnd = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 3600 * 3600).AddDays(chartDayOffset);
        if (demo)
        {
            var samples = new System.Collections.Generic.List<HistoryPoint>();
            for (int i = 0; i < 168; i++) samples.Add(new(chartEnd.AddHours(i - 167), 28 - i * 0.019 + Math.Sin(i / 12.0) * 0.2, 18.6, 82, 96 - i * 0.19));
            historyPoints = samples; return;
        }
        try { historyPoints = history?.Read(chartEnd) ?? Array.Empty<HistoryPoint>(); }
        catch (Exception e) when (e is SqliteStoreException or IOException) { historyPoints = Array.Empty<HistoryPoint>(); status.Text = "History could not be read."; }
    }
    private bool SaveHistory()
    {
        if (history == null) return false;
        try { history.Save(snapshot, DateTimeOffset.UtcNow); LoadHistory(); return true; }
        catch (Exception e) when (e is SqliteStoreException or IOException) { return false; }
    }
}
