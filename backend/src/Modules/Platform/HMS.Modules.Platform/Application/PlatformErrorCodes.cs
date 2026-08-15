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
}
