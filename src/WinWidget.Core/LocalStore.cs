using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WinWidget.Core;

public sealed class LocalStore
{
    public string Root { get; }
    public LocalStore(string? root = null)
    {
        Root = root ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinWidget");
        Directory.CreateDirectory(Root);
    }
    public Preferences LoadPreferences() => Load<Preferences>("preferences.json") ?? new();
    public Snapshot LoadCache() => Load<Snapshot>("cache.json") ?? Snapshot.Empty;
    private T? Load<T>(string name)
    {
        try { return JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(Root, name))); }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { return default; }
    }
    public void SavePreferences(Preferences preferences) => Write("preferences.json", JsonSerializer.SerializeToUtf8Bytes(preferences));
    public void SaveCache(Snapshot snapshot) => Write("cache.json", JsonSerializer.SerializeToUtf8Bytes(snapshot));
    public Secrets LoadSecrets()
    {
        try
        {
            var bytes = ProtectedData.Unprotect(File.ReadAllBytes(Path.Combine(Root, "keys.dpapi")), null, DataProtectionScope.CurrentUser);
            try { return JsonSerializer.Deserialize<Secrets>(bytes) ?? new(); }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }
        catch (Exception e) when (e is IOException or CryptographicException or JsonException or UnauthorizedAccessException) { return new(); }
    }
    public void SaveSecrets(Secrets secrets)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(secrets);
        try { Write("keys.dpapi", ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser)); }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
    private void Write(string name, byte[] contents)
    {
        var destination = Path.Combine(Root, name);
        var temp = destination + ".tmp";
        File.WriteAllBytes(temp, contents);
        File.Move(temp, destination, true);
    }
}
