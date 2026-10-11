using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WinWidget.Core;

namespace WinWidget;

public sealed class SettingsWindow : Window
{
    public SettingsWindow(LocalStore store)
    {
        Title = "WinWidget · Provider settings"; Width = 500; Height = 670;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; ResizeMode = ResizeMode.NoResize;
        Background = WidgetWindow.Brush("#12151C"); Foreground = Brushes.White; FontFamily = new("Segoe UI");
        var prefs = store.LoadPreferences(); var existing = store.LoadSecrets();
        var stack = new StackPanel { Margin = new Thickness(24) };
        Content = new ScrollViewer { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        stack.Children.Add(WidgetWindow.Label("Provider settings", 23, "#F1F3F7", FontWeights.SemiBold));
        stack.Children.Add(WidgetWindow.Label("Keys are encrypted for your Windows account. Blank fields keep existing keys; clear removes them. OpenCode keys are used when no saved key exists.", 12, "#A4ACBA"));
        stack.Children.Add(WidgetWindow.Label("DeepSeek API key"));
        var ds = new PasswordBox { Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(8) }; stack.Children.Add(ds);
        var clearDs = Check(existing.DeepSeek.Length > 0 ? "Clear saved DeepSeek key (configured)" : "Clear saved DeepSeek key", false); stack.Children.Add(clearDs);
        stack.Children.Add(WidgetWindow.Label("MiniMax API key"));
        var mm = new PasswordBox { Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(8) }; stack.Children.Add(mm);
        var clearMm = Check(existing.MiniMax.Length > 0 ? "Clear saved MiniMax key (configured)" : "Clear saved MiniMax key", false); stack.Children.Add(clearMm);
        stack.Children.Add(WidgetWindow.Label("OpenCode auth.json path")); var ocPath = Field(prefs.OpenCodeAuthPath); stack.Children.Add(ocPath);
        stack.Children.Add(WidgetWindow.Label("Codex auth.json path")); var cxPath = Field(prefs.CodexAuthPath); stack.Children.Add(cxPath);
        stack.Children.Add(WidgetWindow.Label("Codex quota uses your existing OAuth session. If it expires, sign in again with Codex. The widget does not renew or modify that session.", 11, "#A4ACBA"));
        var showDs = Check("Show DeepSeek", prefs.ShowDeepSeek); stack.Children.Add(showDs);
        var showMm = Check("Show MiniMax", prefs.ShowMiniMax); stack.Children.Add(showMm);
        var showCx = Check("Show Codex", prefs.ShowCodex); stack.Children.Add(showCx);
        var error = WidgetWindow.Label("", 11, "#DEBC84"); stack.Children.Add(error);
        stack.Children.Add(WidgetWindow.Button("Save settings", (_, _) =>
        {
            try
            {
                if (!Path.IsPathFullyQualified(ocPath.Text.Trim()) || !Path.IsPathFullyQualified(cxPath.Text.Trim())) { error.Text = "Use absolute paths for auth files."; return; }
                store.SaveSecrets(new(clearDs.IsChecked == true ? "" : string.IsNullOrWhiteSpace(ds.Password) ? existing.DeepSeek : ds.Password.Trim(), clearMm.IsChecked == true ? "" : string.IsNullOrWhiteSpace(mm.Password) ? existing.MiniMax : mm.Password.Trim()));
                prefs.OpenCodeAuthPath = ocPath.Text.Trim(); prefs.CodexAuthPath = cxPath.Text.Trim();
                prefs.ShowDeepSeek = showDs.IsChecked == true; prefs.ShowMiniMax = showMm.IsChecked == true; prefs.ShowCodex = showCx.IsChecked == true;
                store.SavePreferences(prefs); DialogResult = true;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or CryptographicException) { error.Text = "Settings could not be saved on this Windows account."; }
        }));
    }
    private static TextBox Field(string text) => new() { Text = text, Padding = new Thickness(7), Margin = new Thickness(0, 0, 0, 12) };
    private static CheckBox Check(string text, bool value) => new() { Content = text, IsChecked = value, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 12) };
}
