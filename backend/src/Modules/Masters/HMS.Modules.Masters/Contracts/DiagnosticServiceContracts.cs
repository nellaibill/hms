using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Contracts;

public record CreateDiagnosticServiceRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    /// <summary>Only Laboratory or Radiology are accepted here — Procedure stays exclusively
    /// on the old DiagnosticTest (enforced by CreateDiagnosticServiceRequestValidator).</summary>
    public DiagnosticTestServiceType ServiceType { get; init; }
    public bool IsOutsourced { get; init; }
    /// <summary>Required when <see cref="IsOutsourced"/> is true.</summary>
    public Guid? ProviderId { get; init; }
    public decimal Price { get; init; }
    /// <summary>What it costs the hospital to run this test once — distinct from
    /// <see cref="Price"/>, the patient-facing charge. Defaults to 0 (not yet costed).</summary>
    public decimal CostPrice { get; init; }
    public bool IsActive { get; init; } = true;

    /// <summary>The reporting doctor's share of <see cref="Price"/> — Radiology only; must be 0
    /// for a Laboratory test (ADR-140).</summary>
    public decimal DoctorCharge { get; init; }

    /// <summary>What a bedside scan adds to the price — null when the service isn't done at the
    /// bedside. Set together with <see cref="BedsideDoctorCharge"/>. Radiology only.</summary>
    public decimal? BedsidePrice { get; init; }

    /// <summary>What a bedside scan adds to the doctor's share. Radiology only.</summary>
    public decimal? BedsideDoctorCharge { get; init; }

    /// <summary>Named alternative rates (e.g. "LH", "MR") billed instead of the standard one.
    /// Radiology only. An option with an <c>Id</c> updates that option; one without is added; an
    /// existing option left out is removed.</summary>
    public IReadOnlyList<DiagnosticServiceRateOptionDto> RateOptions { get; init; } = [];
    /// <summary>What a lab test is run on — one of <see cref="DiagnosticSampleTypes.All"/>, or
    /// null. Laboratory only (ADR-154).</summary>
    public string? SampleType { get; init; }

    /// <summary>Where the service sits in the Price List — lower comes first; null when not
    /// placed yet (ADR-154).</summary>
    public int? SortOrder { get; init; }
}

public record UpdateDiagnosticServiceRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public DiagnosticTestServiceType ServiceType { get; init; }
    public bool IsOutsourced { get; init; }
    public Guid? ProviderId { get; init; }
    public decimal Price { get; init; }
    public decimal CostPrice { get; init; }
    public bool IsActive { get; init; } = true;

    /// <summary>The reporting doctor's share of <see cref="Price"/> — Radiology only; must be 0
    /// for a Laboratory test (ADR-140).</summary>
    public decimal DoctorCharge { get; init; }

    /// <summary>What a bedside scan adds to the price — null when the service isn't done at the
    /// bedside. Set together with <see cref="BedsideDoctorCharge"/>. Radiology only.</summary>
    public decimal? BedsidePrice { get; init; }

    /// <summary>What a bedside scan adds to the doctor's share. Radiology only.</summary>
    public decimal? BedsideDoctorCharge { get; init; }

    /// <summary>Named alternative rates (e.g. "LH", "MR") billed instead of the standard one.
    /// Radiology only. An option with an <c>Id</c> updates that option; one without is added; an
    /// existing option left out is removed.</summary>
    public IReadOnlyList<DiagnosticServiceRateOptionDto> RateOptions { get; init; } = [];
    /// <summary>What a lab test is run on — one of <see cref="DiagnosticSampleTypes.All"/>, or
    /// null. Laboratory only (ADR-154).</summary>
    public string? SampleType { get; init; }

    /// <summary>Where the service sits in the Price List — lower comes first; null when not
    /// placed yet (ADR-154).</summary>
    public int? SortOrder { get; init; }
}

public record DiagnosticServiceResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Guid CategoryId { get; init; }
    public DiagnosticTestServiceType ServiceType { get; init; }
    public bool IsOutsourced { get; init; }
    public Guid? ProviderId { get; init; }
    public decimal Price { get; init; }
    public decimal CostPrice { get; init; }
    public bool IsActive { get; init; }
    public decimal DoctorCharge { get; init; }
    public decimal? BedsidePrice { get; init; }
    public decimal? BedsideDoctorCharge { get; init; }
    public IReadOnlyList<DiagnosticServiceRateOptionDto> RateOptions { get; init; } = [];
    public string? SampleType { get; init; }
    public int? SortOrder { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class DiagnosticServiceListQuery : PagedRequest
{
    public Guid? CategoryId { get; set; }
    public DiagnosticTestServiceType? ServiceType { get; set; }
    public bool? IsOutsourced { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>One rate option on a Radiology service (ADR-140): its own price, doctor's share and
/// running cost. <see cref="Id"/> is null for an option being added.</summary>
public record DiagnosticServiceRateOptionDto
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal DoctorCharge { get; init; }
    public decimal CostPrice { get; init; }
}

/// <summary>The sample types a lab test can be listed under (ADR-154), in the tariff sheet's
/// order — the Price List heads its lab tabs "Blood", "Urine"… with them.</summary>
public static class DiagnosticSampleTypes
{
    public static readonly IReadOnlyList<string> All = ["Blood", "Urine", "Stool", "Semen", "Body Fluids", "Histopathology"];
}
