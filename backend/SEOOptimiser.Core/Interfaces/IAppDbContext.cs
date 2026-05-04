using Microsoft.EntityFrameworkCore;
using SEOOptimiser.Core.Entities;

namespace SEOOptimiser.Core.Interfaces;

public interface IAppDbContext
{
    DbSet<ChatSession> ChatSessions { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<Suggestion> Suggestions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
