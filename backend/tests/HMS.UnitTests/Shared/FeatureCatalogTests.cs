using FluentAssertions;
using HMS.Shared.Kernel;
using Xunit;

namespace HMS.UnitTests.Shared;

/// <summary>Sub-feature resolution (ADR-145): what a hospital can use, from its
/// platform.tenant_features rows.</summary>
public class FeatureCatalogTests
{
    private static List<(string Key, bool IsEnabled)> AllModulesOn() =>
        FeatureCatalog.All.Select(key => (key, true)).ToList();

    [Fact]
    public void SubFeatures_AreListedParentBeforeChild_AndEveryParentExists()
    {
        var seen = new HashSet<string>(FeatureCatalog.All);
        foreach (var sub in FeatureCatalog.SubFeatures)
        {
            seen.Should().Contain(sub.Parent, $"'{sub.Key}' names a parent that must be a module or an earlier sub-feature");
            seen.Add(sub.Key).Should().BeTrue($"'{sub.Key}' must be listed once");
        }
    }

    [Fact]
    public void SubFeatureDependencies_OnlyNameSubFeaturesWithTheSameModule()
    {
        foreach (var (key, needs) in FeatureCatalog.SubFeatureDependencies)
        {
            FeatureCatalog.SubFeatureKeys.Should().Contain(key);
            FeatureCatalog.SubFeatureKeys.Should().Contain(needs);
        }
    }

    [Fact]
    public void ResolveEffective_WithNoRows_GivesEveryModuleAndEverySubFeature()
    {
        FeatureCatalog.ResolveEffective([]).Should().BeEquivalentTo(FeatureCatalog.AllWithSubFeatures);
    }

    [Fact]
    public void ResolveEffective_SubFeatureWithNoRow_IsOn()
    {
        // A hospital whose rows predate sub-features: nothing should disappear for it.
        var effective = FeatureCatalog.ResolveEffective(AllModulesOn());

        effective.Should().Contain("hr.leave-requests").And.Contain("settings.branding.logo");
    }

    [Fact]
    public void ResolveEffective_SubFeatureSwitchedOff_IsLeftOut()
    {
        var rows = AllModulesOn();
        rows.Add(("hr.leave-requests", false));

        var effective = FeatureCatalog.ResolveEffective(rows);

        effective.Should().NotContain("hr.leave-requests");
        effective.Should().Contain("hr").And.Contain("hr.leave-types");
    }

    [Fact]
    public void ResolveEffective_ModuleSwitchedOff_HidesEveryOneOfItsSubFeatures()
    {
        var rows = AllModulesOn().Where(row => row.Key != "hr").ToList();
        rows.Add(("hr", false));

        var effective = FeatureCatalog.ResolveEffective(rows);

        effective.Should().NotContain(key => key == "hr" || key.StartsWith("hr."));
    }

    [Fact]
    public void ResolveEffective_NestedParentSwitchedOff_HidesItsChildren()
    {
        var rows = AllModulesOn();
        rows.Add(("settings.branding", false));

        var effective = FeatureCatalog.ResolveEffective(rows);

        effective.Should().NotContain(key => key.StartsWith("settings.branding"));
        effective.Should().Contain("settings.backup");
    }

    [Fact]
    public void ResolveSelectedModules_AlwaysIncludesMandatoryModules()
    {
        // A hospital created before "settings" existed has rows, but none for it.
        var rows = FeatureCatalog.All.Where(key => key != "settings").Select(key => (key, true)).ToList();

        FeatureCatalog.ResolveSelectedModules(rows).Should().Contain("settings");
    }

    [Fact]
    public void OnWithoutRow_OnlyHoldsOptionalKeysWithNoSchema()
    {
        FeatureCatalog.OnWithoutRow.Should().OnlyContain(key => FeatureCatalog.Optional.Contains(key) && FeatureCatalog.UiOnly.Contains(key));
    }

    [Fact]
    public void ResolveSelectedModules_CloudSyncWithNoRow_IsOn()
    {
        // A hospital whose rows predate the "cloud-sync" key keeps its Cloud Sync page.
        var rows = FeatureCatalog.All.Where(key => key != "cloud-sync").Select(key => (key, true)).ToList();

        FeatureCatalog.ResolveSelectedModules(rows).Should().Contain("cloud-sync");
    }

    [Fact]
    public void ResolveSelectedModules_CloudSyncSwitchedOff_IsOffWithAllItsParts()
    {
        var rows = FeatureCatalog.All.Where(key => key != "cloud-sync").Select(key => (key, true)).ToList();
        rows.Add(("cloud-sync", false));

        FeatureCatalog.ResolveEffective(rows).Should().NotContain(key => key == "cloud-sync" || key.StartsWith("cloud-sync."));
    }

    [Fact]
    public void ResolveSelectedModules_OptionalKeyNotInOnWithoutRow_StillNeedsARow()
    {
        var rows = FeatureCatalog.All.Where(key => key != "hr").Select(key => (key, true)).ToList();

        FeatureCatalog.ResolveSelectedModules(rows).Should().NotContain("hr");
    }

    [Fact]
    public void ResolveSelectedModules_IgnoresSubFeatureRows()
    {
        var rows = new List<(string Key, bool IsEnabled)> { ("hr", true), ("hr.attendance", true) };

        FeatureCatalog.ResolveSelectedModules(rows).Should().NotContain("hr.attendance");
    }

    [Fact]
    public void ResolveSelectedSubFeatures_KeepsAChildsOwnStateWhileItsParentIsOff()
    {
        // The platform editor needs this to restore a module's parts as they were.
        var rows = new List<(string Key, bool IsEnabled)> { ("hr", false), ("hr.attendance", false) };

        var selected = FeatureCatalog.ResolveSelectedSubFeatures(rows);

        selected.Should().Contain("hr.leave-requests").And.NotContain("hr.attendance");
    }
}
