using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SupportAgent.API.Controllers;
using SupportAgent.API.Models.DTOs;
using SupportAgent.API.Services;

namespace SupportAgent.Tests;

public class ChatControllerTests
{
    private readonly Mock<IChatService> _svc = new();

    private ChatController Ctrl()
    {
        return new ChatController(_svc.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } },
            },
        };
    }

    // ── ListSessions ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListSessions_Returns200_WithSessionList()
    {
        var sessions = new List<SessionSummaryDto>
        {
            new("sess-1", "How do I reset my password…", DateTime.UtcNow),
            new("sess-2", "What are your billing options?", DateTime.UtcNow),
        };
        _svc.Setup(s => s.ListSessionsAsync(It.IsAny<int>(), default)).ReturnsAsync(sessions);

        var result = await Ctrl().ListSessions(default);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((List<SessionSummaryDto>)ok.Value!).Should().HaveCount(2);
    }

    // ── GetSession ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSession_Returns200_WhenFound()
    {
        var session = new SessionDto("sess-1",
        [
            new MessageDto("user", "Hello", [], DateTime.UtcNow),
            new MessageDto("assistant", "Hi there!", [], DateTime.UtcNow),
        ], DateTime.UtcNow);

        _svc.Setup(s => s.GetSessionAsync("sess-1", default)).ReturnsAsync(session);

        var result = await Ctrl().GetSession("sess-1", default);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((SessionDto)ok.Value!).SessionId.Should().Be("sess-1");
    }

    [Fact]
    public async Task GetSession_Returns404_WhenNotFound()
    {
        _svc.Setup(s => s.GetSessionAsync("missing", default)).ReturnsAsync((SessionDto?)null);

        var result = await Ctrl().GetSession("missing", default);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ── DeleteSession ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteSession_Returns204_WhenDeleted()
    {
        _svc.Setup(s => s.DeleteSessionAsync("sess-1", default)).ReturnsAsync(true);

        var result = await Ctrl().DeleteSession("sess-1", default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteSession_Returns404_WhenNotFound()
    {
        _svc.Setup(s => s.DeleteSessionAsync("missing", default)).ReturnsAsync(false);

        var result = await Ctrl().DeleteSession("missing", default);

        result.Should().BeOfType<NotFoundResult>();
    }
}
