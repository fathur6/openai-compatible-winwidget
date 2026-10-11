using System.Net;
using System.Text.Json;
using WinWidget.Core;

int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
var now = DateTimeOffset.UtcNow;
using (var json = JsonDocument.Parse("""{"rate_limit":{"primary_window":{"limit_window_seconds":604800,"used_percent":36,"reset_at":2000000000},"secondary_window":{"limit_window_seconds":18000,"used_percent":18,"reset_at":1900000000}}}"""))
{
    var quota = ProviderClient.ParseQuota(json.RootElement, now);
    Check(quota.FiveHour?.Value == 82 && quota.Weekly?.Value == 64, "Reordered quota windows must be classified by duration");
    Check(quota.Weekly?.ResetAt == DateTimeOffset.FromUnixTimeSeconds(2000000000), "Server reset timestamp must be preserved");
}
using (var json = JsonDocument.Parse("""{"rate_limit":{"primary_window":{"limit_window_seconds":18000,"used_percent":0,"reset_at":-1},"secondary_window":{"limit_window_seconds":604800,"used_percent":101}}}"""))
{
    var quota = ProviderClient.ParseQuota(json.RootElement, now);
    Check(quota.FiveHour?.Value == 100 && quota.FiveHour.ResetAt == null && quota.Weekly == null, "Invalid weekly window must not discard valid 5-hour data");
}
using (var json = JsonDocument.Parse("""{"balance_infos":[{"currency":"CNY","total_balance":"200"},{"currency":"USD","total_balance":"12.34"}]}"""))
    Check(ProviderClient.ParseBalance("deepseek", json.RootElement) == 12.34, "USD balance must be selected by currency");
using (var json = JsonDocument.Parse("""{"available_amount":0}"""))
    Check(ProviderClient.ParseBalance("minimax", json.RootElement) == 0, "Zero balance is valid");
using (var json = JsonDocument.Parse("""{"available_amount":"NaN"}"""))
{
    bool rejected = false; try { ProviderClient.ParseBalance("minimax", json.RootElement); } catch (JsonException) { rejected = true; }
    Check(rejected, "Non-finite balance must be rejected");
}
var root = Path.Combine(Path.GetTempPath(), "WinWidgetTests-" + Guid.NewGuid());
Directory.CreateDirectory(root);
try
{
    var store = new LocalStore(root);
    store.SaveSecrets(new("fixture-deepseek", "fixture-minimax"));
    Check(store.LoadSecrets() == new Secrets("fixture-deepseek", "fixture-minimax"), "DPAPI key roundtrip");
    Check(!System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(root, "keys.dpapi"))).Contains("fixture-deepseek"), "Keys must not be stored as plaintext");
    File.WriteAllText(Path.Combine(root, "cache.json"), "invalid JSON");
    Check(store.LoadCache().DeepSeek.Reading == null, "Corrupt cache must recover safely");
    var prefs = new Preferences { OpenCodeAuthPath = Path.Combine(root, "missing.json"), CodexAuthPath = Path.Combine(root, "codex.json") };
    File.WriteAllText(prefs.CodexAuthPath, """{"tokens":{"access_token":"fixture-access","account_id":"fixture-account"}}""");
    var stale = new Reading(50, now.AddHours(-2), now.AddDays(2));
    var prior = new Snapshot(new(stale, null), new(stale, null), new(stale, stale, null));
    using var client = new ProviderClient(new FixtureHandler());
    var result = await client.RefreshAsync(prefs, new("fixture-ds", "fixture-mm"), prior);
    Check(result.DeepSeek.Reading == stale && result.DeepSeek.Error != null, "Failed balance refresh must retain original value and timestamp");
    Check(result.MiniMax.Reading?.Value == 19 && result.MiniMax.Error == null, "Provider errors must be isolated");
    Check(result.Codex.FiveHour?.Value == 90 && result.Codex.Weekly == stale && result.Codex.Error != null, "Partial quota refresh must retain only the missing cached window");
    Check(result.Codex.FiveHour?.ResetAt == null, "Fresh reading must not inherit a stale reset");
    var history = new HistoryStore(root);
    history.Save(prior, now);
    Check(history.Read(now).All(p => p.DeepSeek == null && p.Weekly == null), "Old cached readings must not become fresh snapshots");
    var fresh = new Reading(0, now);
    history.Save(new(new(fresh, null), new(fresh, null), new(fresh, fresh, null)), now);
    Check(history.Read(now).Single().DeepSeek == 0, "Ledger stores valid zero balances");
    history.Save(new(new(new(12, now.AddMinutes(1)), null), new(null, null), new(null, null, null)), now.AddMinutes(1));
    Check(history.Read(now.AddMinutes(1)).Single(p => p.Hour.ToUnixTimeSeconds() / 3600 == now.AddMinutes(1).ToUnixTimeSeconds() / 3600).DeepSeek == 12, "Same-hour snapshots update additively");
    history.Save(new(new(new(99, now.AddMonths(-13)), null), new(null, null), new(null, null, null)), now.AddMonths(-13));
    history.Save(Snapshot.Empty, now);
    Check(history.Read(now, 10000).All(p => p.Hour > now.AddMonths(-12)), "Ledger prunes records older than 12 months");
}
finally
{
    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    Directory.Delete(root, true);
}
Console.WriteLine($"Passed {checks} behavior checks.");

sealed class FixtureHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri!.Host == "api.deepseek.com") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        if (request.RequestUri.Host == "platform.minimax.io") return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"available_amount\":\"19.00\"}") });
        if (request.Headers.Authorization?.Parameter != "fixture-access" || !request.Headers.Contains("ChatGPT-Account-ID")) throw new Exception("Missing Codex auth headers");
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"rate_limit":{"primary_window":{"limit_window_seconds":18000,"used_percent":10}}}""") });
    }
}
