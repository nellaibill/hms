using HMS.Modules.Masters.Contracts;

namespace HMS.Modules.Billing.Application;

/// <summary>One unit's cost on a bill line (ADR-153), and for a scan the doctor's share inside it.</summary>
internal readonly record struct LineCost(decimal UnitCost, decimal? UnitDoctorShare);

/// <summary>
/// The cost rules the reports split money by (ADR-113, ADR-139, ADR-140, ADR-141, ADR-144), worked
/// out once when a line is billed so a later Masters change can't rewrite past days (ADR-153).
/// They match frontend/web/src/features/billing/billingCalculations.ts's resolveItemCostPrice and
/// resolveItemCostSplit, which the reports still use for a line that has no stored cost. Each rule
/// returns null when Masters has no cost for the line ("not yet costed"), never a made-up 0: a
/// line saved without a cost keeps following Masters, so a rate set later still reaches it.
/// </summary>
internal static class LineCostRules
{
    /// <summary>A consultation: what the billed consultant takes for this consultation type
    /// (Masters → Consultants → Consultation Types). An explicit 0 is a real share (a consultant who
    /// takes none of the fee); a type the consultant doesn't offer, or a charge left blank, is
    /// unknown.</summary>
    public static LineCost? Consultation(ConsultantResponse? consultant, Guid consultationTypeId)
    {
        var charge = consultant?.ConsultationTypeCharges.FirstOrDefault(c => c.ConsultationTypeId == consultationTypeId)?.ConsultantCharge;
        return charge is decimal share ? new LineCost(share, null) : null;
    }

    /// <summary>A scan (ADR-140): the doctor's share and the running cost of the rate billed (the
    /// rate option's own when one was picked and still exists, else the service's standard rate),
    /// plus the bedside doctor's share for a bedside scan: the line's own (ADR-144) when it was
    /// worked out, else the service's. Unknown when the service can't be found or neither part
    /// of the rate is costed.</summary>
    public static LineCost? Radiology(DiagnosticServiceResponse? service, Guid? rateOptionId, bool isBedside, decimal? billedBedsideDoctorCharge)
    {
        if (service is null)
        {
            return null;
        }

        var option = rateOptionId is Guid optionId ? service.RateOptions.FirstOrDefault(o => o.Id == optionId) : null;
        var doctorShare = option?.DoctorCharge ?? service.DoctorCharge;
        var runningCost = option?.CostPrice ?? service.CostPrice;
        if (doctorShare <= 0 && runningCost <= 0)
        {
            return null;
        }

        var bedsideDoctorShare = isBedside ? billedBedsideDoctorCharge ?? service.BedsideDoctorCharge ?? 0m : 0m;
        var totalDoctorShare = doctorShare + bedsideDoctorShare;
        return new LineCost(totalDoctorShare + runningCost, totalDoctorShare);
    }

    /// <summary>A single lab test: its running cost. 0 means "not yet costed".</summary>
    public static LineCost? LabTest(DiagnosticServiceResponse? service) => CostPrice(service?.CostPrice);

    /// <summary>A lab package: its own running cost when set; otherwise the total of its tests'
    /// running costs, but only when every test has one, since a part total would understate it.
    /// <paramref name="tests"/> holds the package's tests that could be found.</summary>
    public static LineCost? LabPackage(DiagnosticPackageResponse? package, IReadOnlyDictionary<Guid, DiagnosticServiceResponse> tests)
    {
        if (package is null)
        {
            return null;
        }

        if (package.CostPrice > 0)
        {
            return new LineCost(package.CostPrice, null);
        }

        if (package.Items.Count == 0)
        {
            return null;
        }

        var total = 0m;
        foreach (var item in package.Items)
        {
            if (!tests.TryGetValue(item.ServiceId, out var test) || test.CostPrice <= 0)
            {
                return null;
            }

            total += test.CostPrice;
        }

        return new LineCost(total, null);
    }

    /// <summary>A procedure, injection or file: its test's cost price. 0 means "not yet costed".</summary>
    public static LineCost? CostPrice(decimal? costPrice) => costPrice is decimal cost && cost > 0 ? new LineCost(cost, null) : null;
}
