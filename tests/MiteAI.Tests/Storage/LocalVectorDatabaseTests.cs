using System;
using System.Threading.Tasks;
using MiteAI.Core.Storage;
using Xunit;

namespace MiteAI.Tests.Storage;

public class LocalVectorDatabaseTests
{
    [Fact]
    public async Task VectorDatabase_InsertsAndFindsNearestNeighbor()
    {
        using var db = new LocalVectorDatabase(":memory:");
        await db.InitializeAsync();

        var rec1 = new VectorRecord(
            Id: "doc1",
            Content: "Zero trust privacy engine",
            Source: "app.exe",
            Vector: new float[] { 1.0f, 0.0f, 0.0f },
            CreatedAtUtc: DateTime.UtcNow
        );

        var rec2 = new VectorRecord(
            Id: "doc2",
            Content: "DirectML GPU acceleration",
            Source: "app.exe",
            Vector: new float[] { 0.0f, 1.0f, 0.0f },
            CreatedAtUtc: DateTime.UtcNow
        );

        await db.InsertAsync(rec1);
        await db.InsertAsync(rec2);

        // Query vector close to doc1
        var results = await db.SearchSimilarAsync(new float[] { 0.95f, 0.05f, 0.0f }, limit: 1);

        Assert.Single(results);
        Assert.Equal("doc1", results[0].Id);
        Assert.True(results[0].SimilarityScore > 0.9f);
    }

    [Fact]
    public async Task VectorDatabase_HandlesZeroVectorQuerySafely()
    {
        using var db = new LocalVectorDatabase(":memory:");
        await db.InitializeAsync();

        await db.InsertAsync(new VectorRecord("doc1", "Text", "test", new float[] { 1f, 1f }, DateTime.UtcNow));

        var results = await db.SearchSimilarAsync(new float[] { 0f, 0f }, limit: 5);

        Assert.Single(results);
        Assert.Equal(0f, results[0].SimilarityScore); // Gracefully handles 0 magnitude without NaN
    }
}
