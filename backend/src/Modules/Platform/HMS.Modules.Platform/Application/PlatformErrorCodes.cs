using HMS.Modules.Platform.Application.Abstractions;

namespace HMS.Modules.Platform.Application;

internal static class PlatformErrorCodes
{
    public const string InvalidLogin = "PLATFORM.INVALID_LOGIN";
    public const string DuplicateHospitalCode = "PLATFORM.DUPLICATE_HOSPITAL_CODE";
    public const string DuplicateAdminEmail = "PLATFORM.DUPLICATE_ADMIN_EMAIL";
    public const string ProvisioningFailed = TenantProvisioningErrorCodes.Failed;
    public const string NotFound = "PLATFORM.NOT_FOUND";
    public const string InvalidStatus = "PLATFORM.INVALID_STATUS";
    public const string MigrationFailed = "PLATFORM.MIGRATION_FAILED";
    public const string IdempotencyKeyInProgress = "PLATFORM.IDEMPOTENCY_KEY_IN_PROGRESS";
    public const string IdempotencyKeyReused = "PLATFORM.IDEMPOTENCY_KEY_REUSED";

    /// <summary>The MFA challenge token was missing/unknown/expired/already used — same
    /// generic-failure posture as InvalidLogin, since revealing which of those applies
    /// would tell an attacker whether a guessed token exists.</summary>
    public const string MfaChallengeInvalid = "PLATFORM.MFA_CHALLENGE_INVALID";

    /// <summary>The TOTP code did not match — used by both MFA login verification and the
    /// enable/disable setup endpoints (the caller is already authenticated in the latter
    /// two, so unlike InvalidLogin there's no enumeration risk in a specific code).</summary>
    public const string InvalidMfaCode = "PLATFORM.INVALID_MFA_CODE";

    public const string MfaAlreadyEnabled = "PLATFORM.MFA_ALREADY_ENABLED";
    public const string MfaNotEnabled = "PLATFORM.MFA_NOT_ENABLED";

    /// <summary>DeleteHospitalAsync's confirmation check — the caller-supplied hospital
    /// code didn't match the tenant actually being deleted.</summary>
    public const string ConfirmationMismatch = "PLATFORM.CONFIRMATION_MISMATCH";

    public const string NotDeleted = "PLATFORM.NOT_DELETED";
}
