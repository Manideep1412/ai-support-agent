using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SupportAgent.API.Models.DTOs;
using SupportAgent.API.Services;

namespace SupportAgent.API.Controllers;

/// <summary>AI chat — RAG-powered question answering with streaming Server-Sent Events.</summary>
[ApiController]
[Route("api/chat")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chat;

    private static readonly JsonSerializerOptions _json =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ChatController(IChatService chat) => _chat = chat;

    /// <summary>Ask a question and receive a streamed AI answer via Server-Sent Events (SSE).</summary>
    /// <remarks>
    /// This endpoint uses Retrieval-Augmented Generation (RAG):
    /// 1. The question is embedded via the OpenAI Embeddings API.
    /// 2. The top matching knowledge-base articles are retrieved using MongoDB Atlas Vector Search.
    /// 3. The retrieved context is passed to GPT-4o, which streams a grounded answer.
    ///
    /// **Response format:** <c>text/event-stream</c> — three event types are emitted in sequence:
    ///
    /// ```
    /// event: sources
    /// data: { "sources": [{ "id": "...", "title": "...", "category": "...", "content": "..." }], "sessionId": "..." }
    ///
    /// event: chunk
    /// data: { "text": "partial answer text..." }    ← repeated many times
    ///
    /// event: done
    /// data: {}
    /// ```
    ///
    /// - **sources** arrives first, before any content — use it to render source citations.
    /// - **chunk** events stream the answer token by token as the model generates it.
    /// - **done** signals that the stream is complete.
    ///
    /// To continue an existing session, pass the <c>sessionId</c> returned in the sources event.
    /// Omit it (or pass <c>null</c>) to start a new session.
    ///
    /// Example request body:
    /// ```json
    /// {
    ///   "question": "How do I reset my password?",
    ///   "sessionId": null
    /// }
    /// ```
    /// </remarks>
    /// <param name="req">The question text and optional session ID for conversation continuity.</param>
    /// <param name="ct">Cancellation token — close the connection to cancel the stream.</param>
    /// <response code="200">SSE stream with sources, content chunks, and done signal.</response>
    /// <response code="400">Question is missing or exceeds 2000 characters.</response>
    [HttpPost("ask")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

    /// <summary>Return a summary list of recent chat sessions (most recent first).</summary>
    /// <remarks>
    /// Each entry includes a <c>sessionId</c>, a short preview of the first question asked,
    /// and the session creation timestamp. Use the <c>sessionId</c> to load full message
    /// history via <c>GET /api/chat/sessions/{sessionId}</c>.
    /// </remarks>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">List of session summaries ordered by creation time descending.</response>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(List<SessionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SessionSummaryDto>>> ListSessions(CancellationToken ct)
    {
        var sessions = await _chat.ListSessionsAsync(ct: ct);
        return Ok(sessions);
    }

    /// <summary>Return the full message history for a chat session.</summary>
    /// <remarks>
    /// Each message includes the role (<c>user</c> or <c>assistant</c>), the message content,
    /// any source articles that were cited, and the timestamp.
    ///
    /// Useful for rendering the session history in the admin panel.
    /// </remarks>
    /// <param name="sessionId">The session ID returned from the <c>POST /api/chat/ask</c> SSE stream.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Full session with ordered message history.</response>
    /// <response code="404">No session found with the given ID.</response>
    [HttpGet("sessions/{sessionId}")]
    [ProducesResponseType(typeof(SessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SessionDto>> GetSession(string sessionId, CancellationToken ct)
    {
        var session = await _chat.GetSessionAsync(sessionId, ct);
        return session is null ? NotFound() : Ok(session);
    }

    /// <summary>Permanently delete a chat session and its message history.</summary>
    /// <remarks>
    /// Deletes the session document from MongoDB. This action is irreversible.
    /// Used by the admin panel to clean up old or test sessions.
    /// </remarks>
    /// <param name="sessionId">The session ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Session deleted successfully.</response>
    /// <response code="404">No session found with the given ID.</response>
    [HttpDelete("sessions/{sessionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
