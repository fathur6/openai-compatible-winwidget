using System.Globalization;
using System.Runtime.InteropServices;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace WinWidget.Core;

public record HistoryPoint(DateTimeOffset Hour, double? DeepSeek, double? MiniMax, double? FiveHour, double? Weekly);
public sealed class SqliteStoreException(string message) : Exception(message);

public sealed class HistoryStore
{
    private readonly string path;
    public HistoryStore(string root)
    {
        Directory.CreateDirectory(root);
        path = Path.Combine(root, "history.db");
        using var db = new WindowsSqlite(path);
        db.Execute("CREATE TABLE IF NOT EXISTS samples (hour INTEGER PRIMARY KEY, deepseek REAL, minimax REAL, five_hour REAL, weekly REAL);");
    }
    public void Save(Snapshot snapshot, DateTimeOffset now)
    {
        var hour = now.ToUnixTimeSeconds() / 3600 * 3600;
        // Only generated invariant numeric literals or NULL enter SQL; no paths or user strings.
        string Current(Reading? reading) => reading != null && reading.ObservedAt <= now && reading.ObservedAt.ToUnixTimeSeconds() / 3600 * 3600 == hour && double.IsFinite(reading.Value) ? reading.Value.ToString("R", CultureInfo.InvariantCulture) : "NULL";
        var ds = Current(snapshot.DeepSeek.Reading); var mm = Current(snapshot.MiniMax.Reading);
        var five = Current(snapshot.Codex.FiveHour); var weekly = Current(snapshot.Codex.Weekly);
        var cutoff = now.AddMonths(-12).ToUnixTimeSeconds();
        using var db = new WindowsSqlite(path);
        db.Execute(FormattableString.Invariant($"""
            BEGIN IMMEDIATE;
            INSERT OR IGNORE INTO samples(hour,deepseek,minimax,five_hour,weekly) VALUES({hour},{ds},{mm},{five},{weekly});
            UPDATE samples SET deepseek=COALESCE({ds},deepseek), minimax=COALESCE({mm},minimax), five_hour=COALESCE({five},five_hour), weekly=COALESCE({weekly},weekly) WHERE hour={hour};
            DELETE FROM samples WHERE hour < {cutoff};
            COMMIT;
            """));
    }
    public IReadOnlyList<HistoryPoint> Read(DateTimeOffset end, int hours = 168)
    {
        if (hours < 1) throw new ArgumentOutOfRangeException(nameof(hours));
        var endHour = end.ToUnixTimeSeconds() / 3600 * 3600;
        var startHour = endHour - (hours - 1) * 3600L;
        using var db = new WindowsSqlite(path);
        var rows = db.Read(FormattableString.Invariant($"SELECT hour,deepseek,minimax,five_hour,weekly FROM samples WHERE hour >= {startHour} AND hour <= {endHour} ORDER BY hour"));
        return rows.Select(row => new HistoryPoint(DateTimeOffset.FromUnixTimeSeconds(long.Parse(row[0]!, CultureInfo.InvariantCulture)), Number(row[1]), Number(row[2]), Number(row[3]), Number(row[4]))).ToArray();
    }
    private static double? Number(string? text) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) ? value : null;
}

// Use the Microsoft-signed SQLite shipped with Windows, through a narrow API.
// No third-party SQLite binaries are redistributed or signed using our identity.
internal sealed class WindowsSqlite : IDisposable
{
    private IntPtr handle;
    public WindowsSqlite(string path)
    {
        int result = sqlite3_open(path, out handle);
        if (result != 0) { Dispose(); throw new SqliteStoreException("SQLite database could not be opened."); }
        if (sqlite3_busy_timeout(handle, 5000) != 0) { Dispose(); throw new SqliteStoreException("SQLite timeout could not be configured."); }
    }
    public void Execute(string sql) => Execute(sql, null);
    public List<string?[]> Read(string sql)
    {
        var rows = new List<string?[]>();
        Exception? callbackError = null;
        RowCallback callback = (_, count, values, _) =>
        {
            try
            {
                var row = new string?[count];
                for (int i = 0; i < count; i++)
                {
                    var pointer = Marshal.ReadIntPtr(values, i * IntPtr.Size);
                    row[i] = pointer == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(pointer);
                }
                rows.Add(row); return 0;
            }
            catch (Exception e) { callbackError = e; return 1; }
        };
        Execute(sql, callback);
        GC.KeepAlive(callback);
        if (callbackError != null) throw new SqliteStoreException("SQLite row could not be decoded.");
        return rows;
    }
    private void Execute(string sql, RowCallback? callback)
    {
        int result = sqlite3_exec(handle, sql, callback, IntPtr.Zero, out var error);
        if (error != IntPtr.Zero) sqlite3_free(error);
        if (result != 0) throw new SqliteStoreException($"SQLite operation failed (code {result}).");
    }
    public void Dispose() { if (handle != IntPtr.Zero) { sqlite3_close_v2(handle); handle = IntPtr.Zero; } }
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int RowCallback(IntPtr context, int count, IntPtr values, IntPtr names);
    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    private static extern int sqlite3_open([MarshalAs(UnmanagedType.LPUTF8Str)] string path, out IntPtr database);
    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    private static extern int sqlite3_close_v2(IntPtr database);
    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    private static extern int sqlite3_busy_timeout(IntPtr database, int milliseconds);
    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    private static extern int sqlite3_exec(IntPtr database, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, RowCallback? callback, IntPtr context, out IntPtr error);
    [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.StdCall, ExactSpelling = true)]
    private static extern void sqlite3_free(IntPtr memory);
}
