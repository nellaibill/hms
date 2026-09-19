using HMS.Shared.Kernel;

namespace HMS.Modules.Masters.Domain;

/// <summary>
/// A consultant/doctor a patient visit can be attributed to — closes the gap Patients'
/// PatientRegistration.Consultant left as free text (per its own doc comment: "Free-text
/// placeholders until the Staff module exists to back these with a real consultant/
/// department master"). DepartmentId is optional and same-module (validated by direct
/// repository check, not a service call) since Department now lives here too.
/// </summary>
internal class Consultant : Entity
{
    public string Name { get; private set; } = null!;
    public Guid? DepartmentId { get; private set; }
    public string? Specialization { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>Manual sort weighting for consultant pickers (Registration, Billing, and
    /// anywhere else ConsultantSelect is used) — lower shows first, matching a real-world
    /// "who do we want reception to see at the top of the list" priority rather than plain
    /// alphabetical. Null means "no priority set", which sorts after every prioritized
    /// consultant (see ConsultantRepository.ApplySort). Not a uniqueness-constrained rank —
    /// two consultants can share the same priority and just tie-break alphabetically.</summary>
    public int? Priority { get; private set; }

    /// <summary>Relative path (e.g. "uploads/consultants/{id}.jpg") to this consultant's photo,
    /// set only via UploadPhotoAsync — never part of Create/Update (mirrors User.ProfilePhotoUrl,
    /// which is likewise excluded from UpdateUserRequest). Null until a photo is uploaded.</summary>
    public string? PhotoUrl { get; private set; }

    /// <summary>Days of the week this consultant is available at the hospital, as plain
    /// System.DayOfWeek names (e.g. "Monday") — purely static reference info shown to Reception/
    /// OPD when picking a consultant, not a scheduling engine (no per-day distinct hours, no
    /// conflict/overlap validation). Empty until set. See ConsultantConfiguration for the
    /// Postgres text[] mapping.</summary>
    public IReadOnlyList<string> AvailableDays { get; private set; } = [];

    /// <summary>One shared visiting-hours range applied to every day in AvailableDays — not
    /// per-day. Both null until set; when either is set the other must be too (enforced by
    /// Create/Update's Guard below), and VisitEndTime must be after VisitStartTime.</summary>
    public TimeOnly? VisitStartTime { get; private set; }
    public TimeOnly? VisitEndTime { get; private set; }

    /// <summary>Optional second visiting session (e.g. an evening clinic) — same days as the
    /// first (AvailableDays is shared), a separate time range. Both null until set; both must be
    /// set together, and the end must be after the start. Slot 1 stays the required one.</summary>
    public TimeOnly? VisitStartTime2 { get; private set; }
    public TimeOnly? VisitEndTime2 { get; private set; }

    /// <summary>Which consultation types (billing categories, e.g. "Doctor's Consultation
    /// (In-house) - Regular") this consultant offers, and what the hospital pays them for each
    /// (ConsultantConsultationType.ConsultantCharge) — a many-to-many replaced wholesale on
    /// every Create/Update (see SetConsultationTypes), the same "fully replace, no incremental
    /// add/remove" convention AvailableDays already uses on this same entity.</summary>
    private readonly List<ConsultantConsultationType> _consultationTypes = [];
    public IReadOnlyCollection<ConsultantConsultationType> ConsultationTypes => _consultationTypes;

    // Required by EF Core materialization.
    private Consultant()
    {
    }

    private Consultant(
        Guid id,
        string name,
        Guid? departmentId,
        string? specialization,
        bool isActive,
        int? priority,
        IReadOnlyList<string> availableDays,
        TimeOnly? visitStartTime,
        TimeOnly? visitEndTime,
        TimeOnly? visitStartTime2,
        TimeOnly? visitEndTime2,
        Guid? createdBy)
        : base(id, createdBy)
    {
        VisitStartTime2 = visitStartTime2;
        VisitEndTime2 = visitEndTime2;
        Name = name;
        DepartmentId = departmentId;
        Specialization = specialization;
        IsActive = isActive;
        Priority = priority;
        AvailableDays = availableDays;
        VisitStartTime = visitStartTime;
        VisitEndTime = visitEndTime;
    }

    public static Consultant Create(
        string name,
        Guid? departmentId,
        string? specialization,
        bool isActive,
        int? priority,
        IReadOnlyList<string> availableDays,
        TimeOnly? visitStartTime,
        TimeOnly? visitEndTime,
        IReadOnlyList<ConsultationTypeSelection> consultationTypes,
        Guid? createdBy,
        TimeOnly? visitStartTime2 = null,
        TimeOnly? visitEndTime2 = null)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        GuardAvailability(availableDays, visitStartTime, visitEndTime, visitStartTime2, visitEndTime2);

        var consultant = new Consultant(
            Guid.CreateVersion7(),
            name.Trim(),
            departmentId,
            specialization?.Trim(),
            isActive,
            priority,
            availableDays,
            visitStartTime,
            visitEndTime,
            visitStartTime2,
            visitEndTime2,
            createdBy);

        consultant.SetConsultationTypes(consultationTypes);

        return consultant;
    }

    public void Update(
        string name,
        Guid? departmentId,
        string? specialization,
        bool isActive,
        int? priority,
        IReadOnlyList<string> availableDays,
        TimeOnly? visitStartTime,
        TimeOnly? visitEndTime,
        IReadOnlyList<ConsultationTypeSelection> consultationTypes,
        Guid? updatedBy,
        TimeOnly? visitStartTime2 = null,
        TimeOnly? visitEndTime2 = null)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        GuardAvailability(availableDays, visitStartTime, visitEndTime, visitStartTime2, visitEndTime2);

        Name = name.Trim();
        DepartmentId = departmentId;
        Specialization = specialization?.Trim();
        IsActive = isActive;
        Priority = priority;
        AvailableDays = availableDays;
        VisitStartTime = visitStartTime;
        VisitEndTime = visitEndTime;
        VisitStartTime2 = visitStartTime2;
        VisitEndTime2 = visitEndTime2;
        SetConsultationTypes(consultationTypes);
        MarkUpdated(updatedBy);
    }

    /// <summary>Set only by ConsultantService.UploadPhotoAsync, after the file has already been
    /// written to storage — see Domain/Consultant.cs's own PhotoUrl doc comment.</summary>
    public void SetPhoto(string photoUrl, Guid? updatedBy)
    {
        PhotoUrl = photoUrl;
        MarkUpdated(updatedBy);
    }

    private void SetConsultationTypes(IReadOnlyList<ConsultationTypeSelection> consultationTypes)
    {
        _consultationTypes.Clear();
        foreach (var selection in consultationTypes.DistinctBy(s => s.ConsultationTypeId))
        {
            _consultationTypes.Add(ConsultantConsultationType.Create(Id, selection.ConsultationTypeId, selection.ConsultantCharge));
        }
    }

    /// <summary>Repeats the request-level validator's own rules as a genuine domain invariant
    /// (same split as every other entity in this codebase, e.g. LabOrder.GenerateReport) —
    /// VisitStartTime/VisitEndTime must both be set or both be unset, and the end must be after
    /// the start.</summary>
    private static void GuardAvailability(
        IReadOnlyList<string> availableDays,
        TimeOnly? visitStartTime,
        TimeOnly? visitEndTime,
        TimeOnly? visitStartTime2,
        TimeOnly? visitEndTime2)
    {
        if (visitStartTime2.HasValue != visitEndTime2.HasValue)
        {
            throw new ArgumentException("Second visit start time and end time must both be set, or both left blank.");
        }

        if (visitStartTime2.HasValue && visitEndTime2.HasValue && visitEndTime2.Value <= visitStartTime2.Value)
        {
            throw new ArgumentException("Second visit end time must be after its start time.");
        }

        if (visitStartTime.HasValue != visitEndTime.HasValue)
        {
            throw new ArgumentException("Visit start time and end time must both be set, or both left blank.");
        }

        if (visitStartTime.HasValue && visitEndTime.HasValue && visitEndTime.Value <= visitStartTime.Value)
        {
            throw new ArgumentException("Visit end time must be after the start time.");
        }

        foreach (var day in availableDays)
        {
            if (!Enum.TryParse<DayOfWeek>(day, out _))
            {
                throw new ArgumentException($"'{day}' is not a valid day of the week.");
            }
        }
    }
}

/// <summary>One consultation type to assign to a consultant, with what the hospital pays them
/// for it — see ConsultantConsultationType.ConsultantCharge's own doc comment for why this is
/// per-(consultant, type) rather than a single flat rate.</summary>
internal sealed record ConsultationTypeSelection(Guid ConsultationTypeId, decimal? ConsultantCharge);
