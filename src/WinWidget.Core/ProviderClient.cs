using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace WinWidget.Core;

public sealed class ProviderClient : IDisposable
{
    private readonly HttpClient http;
    public ProviderClient(HttpMessageHandler? handler = null)
    {
        http = handler == null ? new() : new(handler);
        http.Timeout = TimeSpan.FromSeconds(12);
        http.DefaultRequestHeaders.Accept.Add(new("application/json"));
        http.DefaultRequestHeaders.UserAgent.ParseAdd("WinWidget/0.2");
    }
    public async Task<Snapshot> RefreshAsync(Preferences prefs, Secrets keys, Snapshot previous, CancellationToken ct = default)
    {
        var ds = BalanceAsync("deepseek", keys.DeepSeek, prefs.OpenCodeAuthPath, previous.DeepSeek.Reading, ct);
        var mm = BalanceAsync("minimax", keys.MiniMax, prefs.OpenCodeAuthPath, previous.MiniMax.Reading, ct);
        var codex = QuotaAsync(prefs.CodexAuthPath, previous.Codex, ct);
        await Task.WhenAll(ds, mm, codex);
        return new(await ds, await mm, await codex);
    }
    private async Task<ProviderResult> BalanceAsync(string provider, string key, string authPath, Reading? cached, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key)) key = ReadOpenCodeKey(authPath, provider);
            if (string.IsNullOrWhiteSpace(key)) return new(cached, "Add an API key in Settings");
            var url = provider == "deepseek" ? "https://api.deepseek.com/user/balance" : "https://platform.minimax.io/account/query_balance";
            using var payload = await GetAsync(url, key, null, ct);
            return new(new(ParseBalance(provider, payload.RootElement), DateTimeOffset.UtcNow), null);
        }
        catch (Exception e) when (Expected(e)) { return new(cached, SafeError(e)); }
    }
    private async Task<QuotaResult> QuotaAsync(string path, QuotaResult cached, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(path)) return cached with { Error = "Sign in to Codex, then refresh" };
            using var auth = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct));
            if (!auth.RootElement.TryGetProperty("tokens", out var tokens) || !tokens.TryGetProperty("access_token", out var token) || string.IsNullOrWhiteSpace(token.GetString()))
                return cached with { Error = "Codex OAuth session unavailable" };
            var account = tokens.TryGetProperty("account_id", out var id) ? id.GetString() : null;
            using var payload = await GetAsync("https://chatgpt.com/backend-api/wham/usage", token.GetString()!, account, ct);
            var fresh = ParseQuota(payload.RootElement, DateTimeOffset.UtcNow);
            return new(fresh.FiveHour ?? cached.FiveHour, fresh.Weekly ?? cached.Weekly, fresh.Error);
        }
        catch (Exception e) when (Expected(e)) { return cached with { Error = SafeError(e) }; }
    }
    private async Task<JsonDocument> GetAsync(string url, string key, string? account, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key.Trim());
        if (!string.IsNullOrEmpty(account)) request.Headers.Add("ChatGPT-Account-ID", account);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Provider request failed", null, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    }
    public static double ParseBalance(string provider, JsonElement json)
    {
        JsonElement value;
        if (provider == "deepseek")
        {
            if (json.TryGetProperty("is_available", out var available) && available.ValueKind == JsonValueKind.False) throw new JsonException();
            var usd = json.GetProperty("balance_infos").EnumerateArray().FirstOrDefault(i => i.TryGetProperty("currency", out var c) && c.GetString() == "USD");
            if (usd.ValueKind != JsonValueKind.Object) throw new JsonException();
            value = usd.GetProperty("total_balance");
        }
        else value = json.GetProperty("available_amount");
        if (!TryNumber(value, out double result) || result < 0) throw new JsonException();
        return result;
    }
    public static QuotaResult ParseQuota(JsonElement json, DateTimeOffset observed)
    {
        Reading? five = null, weekly = null;
        if (!json.TryGetProperty("rate_limit", out var limits) || limits.ValueKind != JsonValueKind.Object) throw new JsonException();
        foreach (var name in new[] { "primary_window", "secondary_window" })
        {
            if (!limits.TryGetProperty(name, out var window) || window.ValueKind != JsonValueKind.Object) continue;
            if (!window.TryGetProperty("limit_window_seconds", out var duration) || !TryNumber(duration, out double seconds)) continue;
            if (!window.TryGetProperty("used_percent", out var used) || !TryNumber(used, out double percent) || percent < 0 || percent > 100) continue;
            DateTimeOffset? reset = null;
            if (window.TryGetProperty("reset_at", out var timestamp) && TryNumber(timestamp, out double time) && time > 0 && time < 253402300800)
                reset = DateTimeOffset.FromUnixTimeSeconds((long)time);
            var reading = new Reading(100 - percent, observed, reset);
            if (seconds == 18000) five ??= reading;
            if (seconds == 604800) weekly ??= reading;
        }
        return new(five, weekly, five == null || weekly == null ? "Some quota windows unavailable" : null);
    }
    private static bool TryNumber(JsonElement value, out double number)
    {
        number = 0;
        return (value.ValueKind == JsonValueKind.Number ? value.TryGetDouble(out number) : value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number)) && double.IsFinite(number);
    }
    private static string ReadOpenCodeKey(string path, string provider)
    {
        if (!File.Exists(path)) return "";
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        return json.RootElement.TryGetProperty(provider, out var item) && item.TryGetProperty("key", out var key) ? key.GetString() ?? "" : "";
    }
    private static bool Expected(Exception e) => e is HttpRequestException or OperationCanceledException or IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or KeyNotFoundException or FormatException or ArgumentException;
    private static string SafeError(Exception e) => e switch
    {
        HttpRequestException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden } => "Authentication failed · update credentials",
        HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests } => "Rate limited · retry later",
        HttpRequestException => "Provider unavailable · cached reading retained",
        OperationCanceledException => "Request timed out · cached reading retained",
        IOException or UnauthorizedAccessException => "Local auth file could not be read",
        _ => "Unexpected provider response · cached reading retained"
    };
    public void Dispose() => http.Dispose();
}
