using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SupportAgent.API.Models.DTOs;
using SupportAgent.API.Services;

namespace SupportAgent.API.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chat;

    private static readonly JsonSerializerOptions _json =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ChatController(ChatService chat) => _chat = chat;

    /// <summary>
    /// SSE endpoint. Emits three event types:
    ///   event: sources  → { sources: [...], sessionId: "..." }
    ///   event: chunk    → { text: "..." }          (many)
    ///   event: done     → {}
    /// </summary>
    [HttpPost("ask")]
    public async Task Ask([FromBody] AskRequest req, CancellationToken ct)
    {
        Response.Headers["Content-Type"]      = "text/event-stream";
        Response.Headers["Cache-Control"]     = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";    // disable nginx buffering

        var (stream, sources, sessionId) = await _chat.AskAsync(req, ct);

        // 1 – Send sources + session id before the first content chunk
        await WriteSseAsync("sources", new SseSourcesPayload(sources, sessionId), ct);

        // 2 – Stream content
        await foreach (var chunk in stream.WithCancellation(ct))
            await WriteSseAsync("chunk", new SseChunkPayload(chunk), ct);

        // 3 – Signal completion
        await WriteSseAsync("done", new { }, ct);
    }

    /// <summary>Returns a summary list of recent sessions (most recent first).</summary>
    [HttpGet("sessions")]
    public async Task<ActionResult<List<SessionSummaryDto>>> ListSessions(CancellationToken ct)
    {
        var sessions = await _chat.ListSessionsAsync(ct: ct);
        return Ok(sessions);
    }

    /// <summary>Returns the full message history for a session.</summary>
    [HttpGet("sessions/{sessionId}")]
    public async Task<ActionResult<SessionDto>> GetSession(string sessionId, CancellationToken ct)
    {
        var session = await _chat.GetSessionAsync(sessionId, ct);
        return session is null ? NotFound() : Ok(session);
    }

    /// <summary>Deletes a chat session permanently.</summary>
    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(string sessionId, CancellationToken ct)
    {
        var deleted = await _chat.DeleteSessionAsync(sessionId, ct);
        return deleted ? NoContent() : NotFound();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task WriteSseAsync<T>(string eventName, T payload, CancellationToken ct)
    {
        var data = JsonSerializer.Serialize(payload, _json);
        await Response.WriteAsync($"event: {eventName}\ndata: {data}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
