namespace MiteAI.Core.RAG;

public record TextChunk(
    string DocumentId,
    int Index,
    string Content,
    int StartOffset,
    int Length
);
