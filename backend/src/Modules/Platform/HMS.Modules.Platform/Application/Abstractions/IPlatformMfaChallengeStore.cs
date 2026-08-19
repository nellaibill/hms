namespace HMS.Modules.Platform.Application.Abstractions;

/// <summary>
/// The second leg of a two-step Platform Admin login — see PlatformMfaChallenge's own doc
/// comment for the full flow. Internal (unlike IRevokedTokenStore/IProvisioningAlertStore):
/// both ends of this seam (PlatformAuthenticationService, PlatformAuthController) live
/// inside this module, so there's no Api/module boundary to bridge here.
/// </summary>
internal interface IPlatformMfaChallengeStore
{
    /// <summary>Issues a new single-use challenge token for <paramref name="platformUserId"/>,
    /// valid for a short window (minutes, not hours).</summary>
    Task<string> CreateAsync(Guid platformUserId, CancellationToken cancellationToken);

    /// <summary>Non-consuming lookup — returns the associated Platform Admin's id if
    /// <paramref name="token"/> is unexpired and not already consumed, otherwise null. A
    /// wrong TOTP code must not burn the challenge (the caller should be able to retry
    /// until they get the code right or the challenge naturally expires), so verifying the
    /// code and consuming the challenge are deliberately two separate steps — see
    /// <see cref="ConsumeAsync"/>.</summary>
    Task<Guid?> ValidateAsync(string token, CancellationToken cancellationToken);

    /// <summary>Marks <paramref name="token"/> consumed, once and only once the caller has
    /// confirmed the TOTP code was correct — a second verify attempt with the same token
    /// (replayed or otherwise) always fails <see cref="ValidateAsync"/> afterward, even
    /// though the code itself would still compute as valid for the next ~30s.</summary>
    Task ConsumeAsync(string token, CancellationToken cancellationToken);
}
