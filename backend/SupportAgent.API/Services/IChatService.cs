using SupportAgent.API.Models.DTOs;

namespace SupportAgent.API.Services;

public interface IChatService
{
    Task<(IAsyncEnumerable<string> Stream, List<SourceReference> Sources, string SessionId)>
        AskAsync(AskRequest req, CancellationToken ct = default);

    Task<SessionDto?> GetSessionAsync(string sessionId, CancellationToken ct = default);
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken ct = default);
    Task<List<SessionSummaryDto>> ListSessionsAsync(int limit = 30, CancellationToken ct = default);
}
