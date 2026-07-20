using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using SupportAgent.API.Controllers;
using SupportAgent.API.Models.DTOs;
using SupportAgent.API.Services;

namespace SupportAgent.Tests;

public class KnowledgeControllerTests
{
    private readonly Mock<IKnowledgeService> _svc = new();
    private KnowledgeController Ctrl() => new(_svc.Object);

    private static ArticleDto MakeArticle(string id = "abc123") =>
        new(id, "Password Reset", "How to reset...", "Account", true,
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200_WithArticleList()
    {
        var articles = new List<ArticleDto> { MakeArticle("id1"), MakeArticle("id2") };
        _svc.Setup(s => s.GetAllAsync(default)).ReturnsAsync(articles);

        var result = await Ctrl().GetAll(default);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((List<ArticleDto>)ok.Value!).Should().HaveCount(2);
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_Returns200_WhenFound()
    {
        _svc.Setup(s => s.GetByIdAsync("abc123", default)).ReturnsAsync(MakeArticle());

        var result = await Ctrl().GetById("abc123", default);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((ArticleDto)ok.Value!).Id.Should().Be("abc123");
    }

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        _svc.Setup(s => s.GetByIdAsync("missing", default)).ReturnsAsync((ArticleDto?)null);

        var result = await Ctrl().GetById("missing", default);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Returns201_WithCreatedArticle()
    {
        var req     = new CreateArticleRequest("Title", "Content", "General");
        var article = MakeArticle("new-id");
        _svc.Setup(s => s.CreateAsync(req, default)).ReturnsAsync(article);

        var result = await Ctrl().Create(req, default);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        ((ArticleDto)created.Value!).Id.Should().Be("new-id");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Returns200_WhenFound()
    {
        var req     = new UpdateArticleRequest("Updated Title", "Updated Content", "Billing");
        var article = new ArticleDto("abc123", "Updated Title", "Updated Content", "Billing", true, DateTime.UtcNow, DateTime.UtcNow);
        _svc.Setup(s => s.UpdateAsync("abc123", req, default)).ReturnsAsync(article);

        var result = await Ctrl().Update("abc123", req, default);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((ArticleDto)ok.Value!).Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task Update_Returns404_WhenNotFound()
    {
        var req = new UpdateArticleRequest("T", "C", "General");
        _svc.Setup(s => s.UpdateAsync("nope", req, default)).ReturnsAsync((ArticleDto?)null);

        var result = await Ctrl().Update("nope", req, default);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Returns204_WhenDeleted()
    {
        _svc.Setup(s => s.DeleteAsync("abc123", default)).ReturnsAsync(true);

        var result = await Ctrl().Delete("abc123", default);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Returns404_WhenNotFound()
    {
        _svc.Setup(s => s.DeleteAsync("nope", default)).ReturnsAsync(false);

        var result = await Ctrl().Delete("nope", default);

        result.Should().BeOfType<NotFoundResult>();
    }

    // ── EmbedArticle ──────────────────────────────────────────────────────────

    [Fact]
    public async Task EmbedArticle_Returns200_WithEmbeddedArticle()
    {
        var article = MakeArticle("abc123");
        _svc.Setup(s => s.EmbedArticleAsync("abc123", default)).ReturnsAsync(article);

        var result = await Ctrl().EmbedArticle("abc123", default);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ((ArticleDto)ok.Value!).HasEmbedding.Should().BeTrue();
    }

    [Fact]
    public async Task EmbedArticle_Returns404_WhenNotFound()
    {
        _svc.Setup(s => s.EmbedArticleAsync("nope", default)).ReturnsAsync((ArticleDto?)null);

        var result = await Ctrl().EmbedArticle("nope", default);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ── EmbedAll ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task EmbedAll_Returns200_WithCounts()
    {
        _svc.Setup(s => s.EmbedAllAsync(default)).ReturnsAsync(new EmbedAllResult(5, 1));

        var result = await Ctrl().EmbedAll(default);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = (EmbedAllResult)ok.Value!;
        dto.Embedded.Should().Be(5);
        dto.Failed.Should().Be(1);
    }
}
