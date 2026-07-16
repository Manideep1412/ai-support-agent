using Microsoft.AspNetCore.Mvc;
using SupportAgent.API.Models.DTOs;
using SupportAgent.API.Services;

namespace SupportAgent.API.Controllers;

[ApiController]
[Route("api/knowledge")]
public class KnowledgeController : ControllerBase
{
    private readonly KnowledgeService _knowledge;

    public KnowledgeController(KnowledgeService knowledge) => _knowledge = knowledge;

    [HttpGet]
    public async Task<ActionResult<List<ArticleDto>>> GetAll(CancellationToken ct) =>
        Ok(await _knowledge.GetAllAsync(ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<ArticleDto>> GetById(string id, CancellationToken ct)
    {
        var article = await _knowledge.GetByIdAsync(id, ct);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpPost]
    public async Task<ActionResult<ArticleDto>> Create([FromBody] CreateArticleRequest req, CancellationToken ct)
    {
        var article = await _knowledge.CreateAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { id = article.Id }, article);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ArticleDto>> Update(
        string id, [FromBody] UpdateArticleRequest req, CancellationToken ct)
    {
        var article = await _knowledge.UpdateAsync(id, req, ct);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await _knowledge.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Generates and stores an embedding vector for a single article.</summary>
    [HttpPost("{id}/embed")]
    public async Task<ActionResult<ArticleDto>> EmbedArticle(string id, CancellationToken ct)
    {
        var article = await _knowledge.EmbedArticleAsync(id, ct);
        return article is null ? NotFound() : Ok(article);
    }

    /// <summary>
    /// Generates embeddings for ALL articles.
    /// Call this once after setup (and after each bulk import).
    /// </summary>
    [HttpPost("embed-all")]
    public async Task<ActionResult<EmbedAllResult>> EmbedAll(CancellationToken ct) =>
        Ok(await _knowledge.EmbedAllAsync(ct));
}
