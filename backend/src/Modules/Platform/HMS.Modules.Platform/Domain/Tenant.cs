using HMS.Modules.Platform.Contracts;
using HMS.Shared.Kernel;

namespace HMS.Modules.Platform.Domain;

/// <summary>
/// Maps a provisioned hospital to its isolated PostgreSQL database. One row per hospital,
/// written once by TenantProvisioningService (HMS.Api) after that hospital's database has
/// been created and migrated — never edited except for Status (Enable/Disable).
/// </summary>
internal class Tenant : Entity
{
    public string HospitalName { get; private set; } = null!;
    public string HospitalCode { get; private set; } = null!;
    public string MobileNumber { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public string Pincode { get; private set; } = null!;
    public string DatabaseName { get; private set; } = null!;
    public TenantStatus Status { get; private set; }

    // Required by EF Core materialization.
    private Tenant()
    {
    }

    private Tenant(
        Guid id,
        string hospitalName,
        string hospitalCode,
        string mobileNumber,
        string email,
        string address,
        string city,
        string state,
        string pincode,
        string databaseName,
        Guid? createdBy)
        : base(id, createdBy)
    {
        HospitalName = hospitalName;
        HospitalCode = hospitalCode;
        MobileNumber = mobileNumber;
        Email = email;
        Address = address;
        City = city;
        State = state;
        Pincode = pincode;
        DatabaseName = databaseName;
        Status = TenantStatus.Active;
    }

    public static Tenant Create(
        string hospitalName,
        string hospitalCode,
        string mobileNumber,
        string email,
        string address,
        string city,
        string state,
        string pincode,
        string databaseName,
        Guid? createdBy)
    {
        Guard.AgainstNullOrWhiteSpace(hospitalName, nameof(hospitalName));
        Guard.AgainstNullOrWhiteSpace(hospitalCode, nameof(hospitalCode));
        Guard.AgainstNullOrWhiteSpace(mobileNumber, nameof(mobileNumber));
        Guard.AgainstNullOrWhiteSpace(email, nameof(email));
        Guard.AgainstNullOrWhiteSpace(address, nameof(address));
        Guard.AgainstNullOrWhiteSpace(city, nameof(city));
        Guard.AgainstNullOrWhiteSpace(state, nameof(state));
        Guard.AgainstNullOrWhiteSpace(pincode, nameof(pincode));
        Guard.AgainstNullOrWhiteSpace(databaseName, nameof(databaseName));

        return new Tenant(
            Guid.CreateVersion7(),
            hospitalName.Trim(),
            hospitalCode.Trim().ToLowerInvariant(),
            mobileNumber.Trim(),
            email.Trim().ToLowerInvariant(),
            address.Trim(),
            city.Trim(),
            state.Trim(),
            pincode.Trim(),
            databaseName,
            createdBy);
    }

    public void SetStatus(TenantStatus status, Guid? updatedBy)
    {
        Status = status;
        MarkUpdated(updatedBy);
    }
}
