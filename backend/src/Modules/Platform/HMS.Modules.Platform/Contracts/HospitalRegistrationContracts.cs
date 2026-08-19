namespace HMS.Modules.Platform.Contracts;

public record CreateHospitalRequest
{
    public string HospitalName { get; init; } = string.Empty;
    public string HospitalCode { get; init; } = string.Empty;
    public string MobileNumber { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Pincode { get; init; } = string.Empty;

    public string SuperAdminUsername { get; init; } = string.Empty;
    public string SuperAdminFirstName { get; init; } = string.Empty;
    public string SuperAdminLastName { get; init; } = string.Empty;
    public string SuperAdminEmail { get; init; } = string.Empty;
    public string SuperAdminPhoneNumber { get; init; } = string.Empty;
    public string SuperAdminPassword { get; init; } = string.Empty;
}

public record CreateHospitalResponse
{
    public Guid Id { get; init; }
    public string HospitalName { get; init; } = string.Empty;
    public string HospitalCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record TenantListItemResponse
{
    public Guid Id { get; init; }
    public string HospitalName { get; init; } = string.Empty;
    public string HospitalCode { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record TenantListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
}

public record UpdateTenantStatusRequest
{
    public string Status { get; init; } = string.Empty;
}

public record TenantDashboardStatsResponse
{
    public int Total { get; init; }
    public int Active { get; init; }
    public int Inactive { get; init; }
}
