namespace WinWidget.Core;

public record Reading(double Value, DateTimeOffset ObservedAt, DateTimeOffset? ResetAt = null);
public record ProviderResult(Reading? Reading, string? Error);
public record QuotaResult(Reading? FiveHour, Reading? Weekly, string? Error);
public record Snapshot(ProviderResult DeepSeek, ProviderResult MiniMax, QuotaResult Codex)
{
    public static Snapshot Empty => new(new(null, "Add an API key in Settings"), new(null, "Add an API key in Settings"), new(null, null, "Sign in to Codex, then refresh"));
    public static Snapshot Demo
    {
        get { var now = DateTimeOffset.UtcNow; return new(new(new(24.80, now), null), new(new(18.60, now), null), new(new(82, now, now.AddHours(3)), new(64, now, now.AddDays(4)), null)); }
    }
}
public sealed class Preferences
{
    public string OpenCodeAuthPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "opencode", "auth.json");
    public string CodexAuthPath { get; set; } = Path.Combine(Environment.GetEnvironmentVariable("CODEX_HOME") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex"), "auth.json");
    public bool ShowDeepSeek { get; set; } = true;
    public bool ShowMiniMax { get; set; } = true;
    public bool ShowCodex { get; set; } = true;
}
public record Secrets(string DeepSeek = "", string MiniMax = "");
