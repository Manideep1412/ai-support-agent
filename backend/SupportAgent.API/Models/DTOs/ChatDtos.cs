using System.ComponentModel.DataAnnotations;

namespace SupportAgent.API.Models.DTOs;

public record AskRequest(
    [Required, MaxLength(2000)] string Question,
    string? SessionId = null
);

public record SourceReference(string Id, string Title, string Category, string Content);

public record SessionSummaryDto(string SessionId, string Preview, DateTime CreatedAt);

public record MessageDto(
    string Role,
    string Content,
    List<SourceReference> Sources,
    DateTime CreatedAt
);

public record SessionDto(string SessionId, List<MessageDto> Messages, DateTime CreatedAt);

// SSE payloads ────────────────────────────────────────────────────────────────
public record SseSourcesPayload(List<SourceReference> Sources, string SessionId);
public record SseChunkPayload(string Text);
