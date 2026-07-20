using Microsoft.AspNetCore.Mvc;
using SupportAgent.API.Models.DTOs;
using SupportAgent.API.Services;

namespace SupportAgent.API.Controllers;

/// <summary>Knowledge base management — articles used as context for AI-generated answers.</summary>
[ApiController]
[Route("api/knowledge")]
[Produces("application/json")]
public class KnowledgeController : ControllerBase
{
    private readonly IKnowledgeService _knowledge;

    public KnowledgeController(IKnowledgeService knowledge) => _knowledge = knowledge;

    /// <summary>Return all knowledge base articles.</summary>
    /// <remarks>
    /// Returns the full list of articles stored in MongoDB, ordered by creation date descending.
    /// Each article includes a <c>hasEmbedding</c> flag indicating whether a vector embedding
    /// has been generated. Only embedded articles are searchable via the RAG pipeline.
    /// </remarks>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">List of all knowledge base articles.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<ArticleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ArticleDto>>> GetAll(CancellationToken ct) =>
        Ok(await _knowledge.GetAllAsync(ct));

    /// <summary>Return a single knowledge base article by its MongoDB document ID.</summary>
    /// <remarks>
    /// The <c>id</c> parameter is the MongoDB ObjectId string (24-character hex).
    /// </remarks>
    /// <param name="id">The MongoDB ObjectId of the article.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">The requested article.</response>
    /// <response code="404">No article found with the given ID.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArticleDto>> GetById(string id, CancellationToken ct)
    {
        var article = await _knowledge.GetByIdAsync(id, ct);
        return article is null ? NotFound() : Ok(article);
    }

    /// <summary>Create a new knowledge base article.</summary>
    /// <remarks>
    /// Creates the article in MongoDB. The article will **not** be searchable by the AI
    /// until you generate its embedding via <c>POST /api/knowledge/{id}/embed</c> or
    /// <c>POST /api/knowledge/embed-all</c>.
    ///
    /// Example request body:
    /// ```json
    /// {
    ///   "title": "How to reset your password",
    ///   "content": "To reset your password, click 'Forgot Password' on the login page...",
    ///   "category": "Account"
    /// }
    /// ```
    ///
    /// Valid categories: <c>Account</c>, <c>Billing</c>, <c>Technical</c>, <c>General</c>.
    /// </remarks>
    /// <param name="req">Article title, content, and category.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="201">Article created. Location header points to the new resource.</response>
    /// <response code="400">Validation failed (missing title, missing content, or content exceeds 10 000 chars).</response>
    [HttpPost]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ArticleDto>> Create([FromBody] CreateArticleRequest req, CancellationToken ct)
    {
        var article = await _knowledge.CreateAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { id = article.Id }, article);
    }

    /// <summary>Update the title, content, or category of an existing article.</summary>
    /// <remarks>
    /// Performs a full replacement of the article fields (not a partial patch).
    /// After updating, the existing embedding becomes stale — call
    /// <c>POST /api/knowledge/{id}/embed</c> to regenerate it so the AI uses the new content.
    ///
    /// Example request body:
    /// ```json
    /// {
    ///   "title": "How to reset your password (updated)",
    ///   "content": "Updated instructions...",
    ///   "category": "Account"
    /// }
    /// ```
    /// </remarks>
    /// <param name="id">The MongoDB ObjectId of the article to update.</param>
    /// <param name="req">Replacement title, content, and category.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">The updated article.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="404">No article found with the given ID.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArticleDto>> Update(
        string id, [FromBody] UpdateArticleRequest req, CancellationToken ct)
    {
        var article = await _knowledge.UpdateAsync(id, req, ct);
        return article is null ? NotFound() : Ok(article);
    }

    /// <summary>Permanently delete a knowledge base article.</summary>
    /// <remarks>
    /// Removes the article and its stored embedding from MongoDB.
    /// This is a hard delete and cannot be undone.
    /// </remarks>
    /// <param name="id">The MongoDB ObjectId of the article to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Article deleted successfully.</response>
    /// <response code="404">No article found with the given ID.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var deleted = await _knowledge.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Generate and store an OpenAI embedding vector for a single article.</summary>
    /// <remarks>
    /// Calls the OpenAI Embeddings API (<c>text-embedding-3-small</c>) to generate a
    /// 1536-dimension vector for the article's content. The vector is stored in MongoDB
    /// and used by the Atlas Vector Search index to find relevant articles at query time.
    ///
    /// Call this after creating or updating an article to keep its embedding in sync.
    /// </remarks>
    /// <param name="id">The MongoDB ObjectId of the article to embed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Article with <c>hasEmbedding: true</c> confirming success.</response>
    /// <response code="404">No article found with the given ID.</response>
    [HttpPost("{id}/embed")]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArticleDto>> EmbedArticle(string id, CancellationToken ct)
    {
        var article = await _knowledge.EmbedArticleAsync(id, ct);
        return article is null ? NotFound() : Ok(article);
    }

    /// <summary>Generate embeddings for all articles that are missing a vector.</summary>
    /// <remarks>
    /// Iterates over every article in the knowledge base and calls the OpenAI Embeddings API
    /// for any that do not yet have a stored vector. Articles that already have an embedding
    /// are skipped to avoid unnecessary API calls and cost.
    ///
    /// **Call this once after initial setup** (seeder run) or after a bulk import.
    /// For individual articles after create/update, prefer <c>POST /api/knowledge/{id}/embed</c>.
    ///
    /// Returns a summary of how many articles were successfully embedded and how many failed.
    /// </remarks>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Summary with <c>embedded</c> (success count) and <c>failed</c> (error count).</response>
    [HttpPost("embed-all")]
    [ProducesResponseType(typeof(EmbedAllResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmbedAllResult>> EmbedAll(CancellationToken ct) =>
        Ok(await _knowledge.EmbedAllAsync(ct));
}
