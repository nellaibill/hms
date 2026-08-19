using System.Security.Cryptography;
using HMS.Modules.Platform.Application.Abstractions;
using HMS.Modules.Platform.Domain;
using Microsoft.EntityFrameworkCore;

namespace HMS.Modules.Platform.Infrastructure;

internal sealed class PlatformMfaChallengeStore : IPlatformMfaChallengeStore
{
    // Deliberately short — this only ever proves "the password step just passed a moment
    // ago," not a real session. See PlatformMfaChallenge's own doc comment.
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    private readonly PlatformDbContext _dbContext;

    public PlatformMfaChallengeStore(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> CreateAsync(Guid platformUserId, CancellationToken cancellationToken)
    {
        var token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var challenge = PlatformMfaChallenge.Create(platformUserId, token, DateTime.UtcNow.Add(ChallengeLifetime));

        await _dbContext.PlatformMfaChallenges.AddAsync(challenge, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return token;
    }

    public async Task<Guid?> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        var challenge = await _dbContext.PlatformMfaChallenges.FirstOrDefaultAsync(c => c.Token == token, cancellationToken);
        return challenge is not null && challenge.IsUsable(DateTime.UtcNow) ? challenge.PlatformUserId : null;
    }

    public async Task ConsumeAsync(string token, CancellationToken cancellationToken)
    {
        var challenge = await _dbContext.PlatformMfaChallenges.FirstOrDefaultAsync(c => c.Token == token, cancellationToken);
        if (challenge is null)
        {
            return;
        }

        challenge.Consume();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
