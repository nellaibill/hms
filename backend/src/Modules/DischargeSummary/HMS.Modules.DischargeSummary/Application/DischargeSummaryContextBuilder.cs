using System.Globalization;
using System.Text;
using HMS.Modules.IPD.Contracts;

namespace HMS.Modules.DischargeSummary.Application;

/// <summary>
/// Renders an admission's IPD data as plain text for the AI provider. Deliberately carries no
/// patient name, UHID or phone number (age/gender only) — the model doesn't need them to write a
/// clinical narrative, and they'd otherwise leave the system for nothing.
/// </summary>
internal static class DischargeSummaryContextBuilder
{
    internal const int MaxNotesCharacters = 40_000;
    private const int LeadingNotesKept = 3;

    public static string Build(
        AdmissionResponse admission,
        IReadOnlyList<ProgressNoteResponse> progressNotes,
        IReadOnlyList<VitalsReadingResponse> vitals,
        IReadOnlyList<DoctorOrderResponse> doctorOrders,
        IReadOnlyList<MedicationOrderResponse> medicationOrders)
    {
        var text = new StringBuilder();

        text.AppendLine("ADMISSION");
        text.AppendLine($"Patient: {admission.Age} years, {admission.Gender}");
        text.AppendLine($"Admission type: {admission.AdmissionType}; ward: {admission.WardName}");
        text.AppendLine($"Admitted: {Date(admission.AdmissionDateTime)}" + (admission.DischargeDateTime is { } discharged
            ? $"; discharged: {Date(discharged)} ({Math.Max(1, (discharged.Date - admission.AdmissionDateTime.Date).Days)} day(s)); discharge type: {admission.DischargeType}"
            : string.Empty));
        AppendLine(text, "Reason for admission", admission.ReasonForAdmission);
        AppendLine(text, "Final diagnosis", admission.FinalDiagnosis);
        AppendLine(text, "Discharge notes", admission.DischargeNotes);
        AppendLine(text, "Follow-up advice recorded at discharge", admission.FollowUpAdvice);

        AppendVitals(text, vitals);
        AppendDoctorOrders(text, doctorOrders);
        AppendMedicationOrders(text, medicationOrders);
        AppendProgressNotes(text, progressNotes);

        return text.ToString();
    }

    private static void AppendVitals(StringBuilder text, IReadOnlyList<VitalsReadingResponse> vitals)
    {
        if (vitals.Count == 0) return;

        var ordered = vitals.OrderBy(v => v.RecordedAt).ToList();
        text.AppendLine();
        text.AppendLine($"VITALS ({ordered.Count} reading(s); first and last shown)");
        text.AppendLine($"First: {VitalsLine(ordered[0])}");
        if (ordered.Count > 1) text.AppendLine($"Last: {VitalsLine(ordered[^1])}");
    }

    private static string VitalsLine(VitalsReadingResponse v)
    {
        var parts = new List<string> { Date(v.RecordedAt) };
        if (v.TemperatureF is not null) parts.Add($"Temp {v.TemperatureF}F");
        if (v.PulseRate is not null) parts.Add($"Pulse {v.PulseRate}");
        if (v.RespiratoryRate is not null) parts.Add($"RR {v.RespiratoryRate}");
        if (v.BloodPressureSystolic is not null && v.BloodPressureDiastolic is not null) parts.Add($"BP {v.BloodPressureSystolic}/{v.BloodPressureDiastolic}");
        if (v.SpO2Percent is not null) parts.Add($"SpO2 {v.SpO2Percent}%");
        if (v.BloodGlucoseMgDl is not null) parts.Add($"Glucose {v.BloodGlucoseMgDl} mg/dL");
        if (!string.IsNullOrWhiteSpace(v.Notes)) parts.Add(v.Notes!.Trim());
        return string.Join(", ", parts);
    }

    private static void AppendDoctorOrders(StringBuilder text, IReadOnlyList<DoctorOrderResponse> orders)
    {
        if (orders.Count == 0) return;

        text.AppendLine();
        text.AppendLine("DOCTOR ORDERS");
        foreach (var order in orders.OrderBy(o => o.OrderedAt))
        {
            text.Append($"- {Date(order.OrderedAt)} {order.OrderType}: {order.Description} [{order.Status}]");
            if (!string.IsNullOrWhiteSpace(order.Instructions)) text.Append($" — {order.Instructions!.Trim()}");
            text.AppendLine();
        }
    }

    private static void AppendMedicationOrders(StringBuilder text, IReadOnlyList<MedicationOrderResponse> orders)
    {
        if (orders.Count == 0) return;

        text.AppendLine();
        text.AppendLine("INPATIENT MEDICATION ORDERS (given during the stay — these are NOT discharge prescriptions)");
        foreach (var order in orders.OrderBy(o => o.StartDate))
        {
            text.Append($"- {order.DrugName} {order.Dose} {order.Route} {order.Frequency}, from {Date(order.StartDate)}");
            if (order.EndDate is { } end) text.Append($" to {Date(end)}");
            text.Append($" [{order.Status}]");
            if (!string.IsNullOrWhiteSpace(order.DiscontinuedReason)) text.Append($" (stopped: {order.DiscontinuedReason!.Trim()})");
            text.AppendLine();
        }
    }

    private static void AppendProgressNotes(StringBuilder text, IReadOnlyList<ProgressNoteResponse> notes)
    {
        if (notes.Count == 0) return;

        var lines = notes.OrderBy(n => n.NoteDateTime).Select(NoteLine).ToList();

        text.AppendLine();
        text.AppendLine($"PROGRESS NOTES ({lines.Count}, chronological)");

        if (lines.Sum(l => l.Length) <= MaxNotesCharacters)
        {
            foreach (var line in lines) text.AppendLine(line);
            return;
        }

        // Too long for one request: keep the admission's opening notes and as many of the most
        // recent ones as fit, and say plainly that the middle was dropped.
        var head = lines.Take(LeadingNotesKept).ToList();
        var budget = MaxNotesCharacters - head.Sum(l => l.Length);
        var tail = new List<string>();
        foreach (var line in Enumerable.Reverse(lines.Skip(LeadingNotesKept)))
        {
            if (line.Length > budget) break;
            budget -= line.Length;
            tail.Insert(0, line);
        }

        foreach (var line in head) text.AppendLine(line);
        text.AppendLine($"[{lines.Count - head.Count - tail.Count} note(s) omitted for length]");
        foreach (var line in tail) text.AppendLine(line);
    }

    private static string NoteLine(ProgressNoteResponse note)
    {
        var parts = new List<string>();
        Add(parts, "Condition", note.ClinicalCondition);
        Add(parts, "Progress", note.Progress);
        Add(parts, "Diagnosis", note.Diagnosis);
        Add(parts, "Assessment", note.Assessment);
        Add(parts, "Plan", note.Plan);
        Add(parts, "Instructions", note.Instructions);
        return $"- {Date(note.NoteDateTime)}: {string.Join("; ", parts)}";
    }

    private static void Add(List<string> parts, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) parts.Add($"{label}: {value.Trim()}");
    }

    private static void AppendLine(StringBuilder text, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) text.AppendLine($"{label}: {value.Trim()}");
    }

    private static string Date(DateTime value) => value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
}
