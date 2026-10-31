using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace MiteAI.Core.Storage;

public class LocalVectorDatabase : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;
    private bool _disposed = false;

    public LocalVectorDatabase(string dbPath = "mite_storage.db")
    {
        _connectionString = dbPath == ":memory:" 
            ? $"Data Source=InMemory_{Guid.NewGuid():N};Mode=Memory;Cache=Shared"
            : $"Data Source={dbPath}";
    }

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync();

        var sql = @"
            CREATE TABLE IF NOT EXISTS vector_records (
                id TEXT PRIMARY KEY,
                content TEXT NOT NULL,
                source TEXT NOT NULL,
                vector BLOB NOT NULL,
                created_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_records_created ON vector_records(created_at);
        ";

        using var cmd = new SqliteCommand(sql, _connection);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task InsertAsync(VectorRecord record)
    {
        EnsureOpen();

        byte[] vectorBytes = new byte[record.Vector.Length * sizeof(float)];
        Buffer.BlockCopy(record.Vector, 0, vectorBytes, 0, vectorBytes.Length);

        var sql = @"
            INSERT OR REPLACE INTO vector_records (id, content, source, vector, created_at)
            VALUES (@id, @content, @source, @vector, @created_at);
        ";

        using var cmd = new SqliteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@id", record.Id);
        cmd.Parameters.AddWithValue("@content", record.Content);
        cmd.Parameters.AddWithValue("@source", record.Source);
        cmd.Parameters.AddWithValue("@vector", vectorBytes);
        cmd.Parameters.AddWithValue("@created_at", record.CreatedAtUtc.ToString("o"));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<SearchResult>> SearchSimilarAsync(float[] queryVector, int limit = 5)
    {
        EnsureOpen();

        var sql = "SELECT id, content, source, vector, created_at FROM vector_records";
        using var cmd = new SqliteCommand(sql, _connection);
        using var reader = await cmd.ExecuteReaderAsync();

        var candidates = new List<SearchResult>();

        while (await reader.ReadAsync())
        {
            var id = reader.GetString(0);
            var content = reader.GetString(1);
            var source = reader.GetString(2);
            var blob = (byte[])reader[3];
            var createdAt = DateTime.Parse(reader.GetString(4));

            var recordVector = new float[blob.Length / sizeof(float)];
            Buffer.BlockCopy(blob, 0, recordVector, 0, blob.Length);

            float score = ComputeCosineSimilarity(queryVector, recordVector);
            candidates.Add(new SearchResult(id, content, source, score, createdAt));
        }

        candidates.Sort((a, b) => b.SimilarityScore.CompareTo(a.SimilarityScore));
        return candidates.Count > limit ? candidates.GetRange(0, limit) : candidates;
    }

    private static float ComputeCosineSimilarity(float[] a, float[] b)
    {
        if (a == null || b == null || a.Length == 0 || b.Length == 0) return 0f;

        int len = Math.Min(a.Length, b.Length);
        float dot = 0f;
        float normA = 0f;
        float normB = 0f;

        for (int i = 0; i < len; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA <= 0f || normB <= 0f) return 0f;

        return dot / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
    }

    private void EnsureOpen()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            throw new InvalidOperationException("LocalVectorDatabase is not initialized. Call InitializeAsync first.");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
