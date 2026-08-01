namespace LocalLlmConsole.Services;

public sealed record HistoryRecord(
    long Id,
    string Category,
    string Message,
    string DataJson,
    DateTimeOffset CreatedAt);

public sealed partial class StateStore
{
    public async Task AppendHistoryAsync(
        string category,
        string message,
        object? data = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("History category is required.", nameof(category));

        await WithConnectionAsync(async () =>
        {
            await using var command = _connection.CreateCommand();
            command.CommandText = """
INSERT INTO history (category, message, data_json, created_at)
VALUES ($category, $message, $data_json, $created_at);
DELETE FROM history
WHERE category = $category
  AND id NOT IN (
    SELECT id FROM history WHERE category = $category ORDER BY id DESC LIMIT 2000
  );
""";
            command.Parameters.AddWithValue("$category", category.Trim());
            command.Parameters.AddWithValue("$message", message?.Trim() ?? "");
            command.Parameters.AddWithValue("$data_json", JsonSerializer.Serialize(data ?? new { }));
            command.Parameters.AddWithValue("$created_at", DateTimeOffset.UtcNow.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        });
    }

    public async Task<IReadOnlyList<HistoryRecord>> ListHistoryAsync(
        string category,
        int limit = 200,
        CancellationToken cancellationToken = default)
    {
        return await WithConnectionAsync<IReadOnlyList<HistoryRecord>>(async () =>
        {
            var records = new List<HistoryRecord>();
            await using var command = _connection.CreateCommand();
            command.CommandText = """
SELECT id, category, message, data_json, created_at
FROM history
WHERE category = $category
ORDER BY id DESC
LIMIT $limit;
""";
            command.Parameters.AddWithValue("$category", category?.Trim() ?? "");
            command.Parameters.AddWithValue("$limit", Math.Clamp(limit, 1, 2000));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                records.Add(new HistoryRecord(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    DateValue(reader.GetString(4))));
            }
            return records;
        });
    }
}
