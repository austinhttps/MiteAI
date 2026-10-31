using System;
using System.Collections.Generic;

namespace MiteAI.Core.RAG;

public class TextChunkingService
{
    private readonly int _chunkSize;
    private readonly int _overlap;

    public TextChunkingService(int chunkSize = 500, int overlap = 50)
    {
        if (chunkSize <= 0)
        {
            throw new ArgumentException("Chunk size must be greater than zero.", nameof(chunkSize));
        }

        if (overlap < 0 || overlap >= chunkSize)
        {
            throw new ArgumentException("Overlap must be non-negative and less than chunk size.", nameof(overlap));
        }

        _chunkSize = chunkSize;
        _overlap = overlap;
    }

    public IEnumerable<TextChunk> ChunkText(string? text, string sourceDocumentId)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        int start = 0;
        int chunkIndex = 0;

        while (start < text.Length)
        {
            int remaining = text.Length - start;
            int length = Math.Min(_chunkSize, remaining);

            // Attempt to break at the last word boundary (space or newline) if not at end of text
            if (length < remaining)
            {
                int lastSpace = text.LastIndexOfAny(new[] { ' ', '\r', '\n', '\t' }, start + length - 1, Math.Min(length, _chunkSize / 2));
                if (lastSpace > start)
                {
                    length = lastSpace - start;
                }
            }

            var content = text.Substring(start, length).Trim();

            if (content.Length > 0)
            {
                yield return new TextChunk(
                    DocumentId: sourceDocumentId ?? "unknown",
                    Index: chunkIndex++,
                    Content: content,
                    StartOffset: start,
                    Length: length
                );
            }

            if (start + length >= text.Length)
            {
                break;
            }

            // Advance start with overlap, ensuring forward progress
            int nextStart = start + Math.Max(1, length - _overlap);
            if (nextStart <= start)
            {
                nextStart = start + length;
            }
            start = nextStart;
        }
    }
}
