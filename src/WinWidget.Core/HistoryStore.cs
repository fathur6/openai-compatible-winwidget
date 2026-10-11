using Microsoft.Data.Sqlite;

namespace WinWidget.Core;

public record HistoryPoint(DateTimeOffset Hour, double? DeepSeek, double? MiniMax, double? FiveHour, double? Weekly);

public sealed class HistoryStore
{
    private readonly string connectionString;
    public HistoryStore(string root)
    {
        connectionString = new SqliteConnectionStringBuilder { DataSource = Path.Combine(root, "history.db"), Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
        using var db = Open();
        using var command = db.CreateCommand();
        command.CommandText = "CREATE TABLE IF NOT EXISTS samples (hour INTEGER PRIMARY KEY, deepseek REAL, minimax REAL, five_hour REAL, weekly REAL);";
        command.ExecuteNonQuery();
    }
    private SqliteConnection Open() { var db = new SqliteConnection(connectionString); db.Open(); return db; }
    public void Save(Snapshot snapshot, DateTimeOffset now)
    {
        // Record only observations made in this hour. Failed refreshes never turn stale cache into new history.
        var hour = now.ToUnixTimeSeconds() / 3600 * 3600;
        object Current(Reading? reading) => reading != null && reading.ObservedAt <= now && reading.ObservedAt.ToUnixTimeSeconds() / 3600 * 3600 == hour && double.IsFinite(reading.Value) ? reading.Value : DBNull.Value;
        using var db = Open(); using var tx = db.BeginTransaction(); using var command = db.CreateCommand(); command.Transaction = tx;
        command.CommandText = """
            INSERT INTO samples(hour, deepseek, minimax, five_hour, weekly) VALUES($hour,$ds,$mm,$five,$weekly)
            ON CONFLICT(hour) DO UPDATE SET deepseek=COALESCE(excluded.deepseek,samples.deepseek), minimax=COALESCE(excluded.minimax,samples.minimax), five_hour=COALESCE(excluded.five_hour,samples.five_hour), weekly=COALESCE(excluded.weekly,samples.weekly);
            DELETE FROM samples WHERE hour < $cutoff;
            """;
        command.Parameters.AddWithValue("$hour", hour);
        command.Parameters.AddWithValue("$ds", Current(snapshot.DeepSeek.Reading));
        command.Parameters.AddWithValue("$mm", Current(snapshot.MiniMax.Reading));
        command.Parameters.AddWithValue("$five", Current(snapshot.Codex.FiveHour));
        command.Parameters.AddWithValue("$weekly", Current(snapshot.Codex.Weekly));
        command.Parameters.AddWithValue("$cutoff", now.AddMonths(-12).ToUnixTimeSeconds());
        command.ExecuteNonQuery(); tx.Commit();
    }
    public IReadOnlyList<HistoryPoint> Read(DateTimeOffset end, int hours = 168)
    {
        var endHour = end.ToUnixTimeSeconds() / 3600 * 3600;
        using var db = Open(); using var command = db.CreateCommand();
        command.CommandText = "SELECT hour,deepseek,minimax,five_hour,weekly FROM samples WHERE hour >= $start AND hour <= $end ORDER BY hour";
        command.Parameters.AddWithValue("$start", endHour - (hours - 1) * 3600L); command.Parameters.AddWithValue("$end", endHour);
        using var reader = command.ExecuteReader(); var result = new List<HistoryPoint>();
        while (reader.Read()) result.Add(new(DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(0)), Number(reader, 1), Number(reader, 2), Number(reader, 3), Number(reader, 4)));
        return result;
    }
    private static double? Number(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetDouble(index);
}
