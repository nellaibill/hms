using FluentAssertions;
using HMS.Modules.Patients.Contracts;
using HMS.Modules.Patients.Domain;
using HMS.Modules.Patients.Infrastructure;
using HMS.Modules.Patients.Infrastructure.Sync;
using HMS.Shared.Kernel.Sync;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HMS.UnitTests.Modules.Patients.Infrastructure;

/// <summary>Offline-first sync capture (ADR-133) on the Patients side, without a database: which
/// operation a save is recorded as, which field names are listed, and that the snapshot
/// round-trips onto another copy of the aggregate.</summary>
public class PatientSyncCaptureTests
{
    private static readonly Guid StateId = Guid.NewGuid();
    private static readonly Guid DistrictId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();

    // Change tracking needs a provider but never a connection.
    private static PatientsDbContext NewContext() =>
        new(new DbContextOptionsBuilder<PatientsDbContext>().UseNpgsql("Host=unused;Database=unused").Options);

    private static Patient NewPatient()
    {
        var patient = Patient.Create(
            "P-2026-040001", Title.Mr, "Ravi", "Kumar", new DateOnly(1980, 5, 17), Gender.Male, BloodGroup.OPositive,
            MaritalStatus.Married, "9876543210", null, "Ravi@Example.com", "Teacher", IdProofType.Aadhaar, "1234",
            ModeOfArrivalSource.DoctorReferral, "Dr A", null, createdBy: Actor);
        patient.SetAddress(Address.Create(patient.Id, "12 Main Road", null, null, StateId, DistrictId, "627001"));
        patient.AddAllergy(Allergy.Create(patient.Id, AllergyType.Drug, "Penicillin", AllergySeverity.Severe), Actor);
        patient.AddEmergencyContact(EmergencyContact.Create(patient.Id, Relationship.Spouse, "Meena", "9876500000"), Actor);
        return patient;
    }

    [Fact]
    public void NewPatient_IsCapturedAsOneInsert()
    {
        using var context = NewContext();
        var patient = NewPatient();
        context.Add(patient);

        var changes = PatientChangeCollector.Collect(context.ChangeTracker);

        changes.Should().ContainSingle();
        changes[patient.Id].Operation.Should().Be(OutboxOperationTypes.Insert);
        changes[patient.Id].ActorId.Should().Be(Actor);
    }

    [Fact]
    public void Edit_IsCapturedAsAnUpdateListingFieldNamesOnly()
    {
        using var context = NewContext();
        var patient = NewPatient();
        context.Attach(patient);

        patient.UpdateDemographics(Title.Mr, "Ravi", "Shankar", patient.DateOfBirth, patient.Gender, patient.BloodGroup, patient.MaritalStatus, Actor);
        patient.UpdateAddress("14 Main Road", null, null, StateId, DistrictId, "627001", Actor);

        var change = PatientChangeCollector.Collect(context.ChangeTracker)[patient.Id];

        change.Operation.Should().Be(OutboxOperationTypes.Update);
        change.Fields.Should().BeEquivalentTo(["LastName", "Address"]);
        change.Fields.Should().NotContain(["UpdatedAt", "UpdatedBy"], "bookkeeping columns say nothing about the edit");
    }

    [Fact]
    public void SoftDelete_IsCapturedAsADelete()
    {
        using var context = NewContext();
        var patient = NewPatient();
        context.Attach(patient);

        patient.SoftDelete(Actor);

        var change = PatientChangeCollector.Collect(context.ChangeTracker)[patient.Id];
        change.Operation.Should().Be(OutboxOperationTypes.Delete);
        change.ActorId.Should().Be(Actor);
    }

    [Fact]
    public void ChildChanges_AreCapturedAgainstTheirPatient()
    {
        using var context = NewContext();
        var patient = NewPatient();
        context.Attach(patient);

        patient.AddAllergy(Allergy.Create(patient.Id, AllergyType.Food, "Peanut", AllergySeverity.Mild), Actor);
        patient.RemoveEmergencyContact(patient.EmergencyContacts.Single().Id, Actor).Should().BeFalse("the last contact cannot be removed");
        patient.AddEmergencyContact(EmergencyContact.Create(patient.Id, Relationship.Son, "Arun", "9876511111"), Actor);

        var change = PatientChangeCollector.Collect(context.ChangeTracker)[patient.Id];
        change.Operation.Should().Be(OutboxOperationTypes.Update);
        change.Fields.Should().Contain(["Allergies", "EmergencyContacts"]);
    }

    [Fact]
    public void UnrelatedSave_CapturesNothing()
    {
        using var context = NewContext();
        context.Attach(NewPatient());

        PatientChangeCollector.Collect(context.ChangeTracker).Should().BeEmpty();
    }

    [Fact]
    public void Snapshot_RoundTripsOntoAnotherCopy_IncludingIdsAuditAndChildren()
    {
        var source = NewPatient();
        source.UpdateContact("9876543219", "9000000000", "new@example.com", null, Actor);
        var json = PatientSyncSnapshotMapper.Serialize(PatientSyncSnapshotMapper.ToSnapshot(source));

        PatientSyncSnapshotMapper.TryParse(json, out var state).Succeeded.Should().BeTrue();
        var replica = Patient.CreateReplica(state!);

        PatientSyncSnapshotMapper.Serialize(PatientSyncSnapshotMapper.ToSnapshot(replica)).Should().Be(json);
        replica.Id.Should().Be(source.Id);
        replica.Allergies.Single().Id.Should().Be(source.Allergies.Single().Id);
    }

    [Fact]
    public void ApplyReplicaState_ReplacesChildrenWithTheSourceLists()
    {
        var replica = NewPatient();
        var source = Patient.CreateReplica(ParseState(NewPatientWithIdOf(replica)));
        source.AddAllergy(Allergy.Create(source.Id, AllergyType.Food, "Egg", AllergySeverity.Mild), Actor);
        var keptAllergy = source.Allergies.Last();

        PatientSyncSnapshotMapper.TryParse(PatientSyncSnapshotMapper.Serialize(PatientSyncSnapshotMapper.ToSnapshot(source)), out var state);
        replica.ApplyReplicaState(state!);

        replica.Allergies.Select(a => a.Id).Should().BeEquivalentTo(source.Allergies.Select(a => a.Id));
        replica.Allergies.Should().Contain(a => a.Id == keptAllergy.Id && a.Specify == "Egg");
        replica.EmergencyContacts.Select(c => c.Id).Should().BeEquivalentTo(source.EmergencyContacts.Select(c => c.Id));
    }

    [Theory]
    [InlineData("\"title\":\"Mr\"", "\"title\":\"King\"")]
    [InlineData("\"gender\":\"Male\"", "\"gender\":\"1\"")]
    public void TryParse_RejectsUnknownEnumValues_WithoutEchoingThem(string from, string to)
    {
        var json = PatientSyncSnapshotMapper.Serialize(PatientSyncSnapshotMapper.ToSnapshot(NewPatient())).Replace(from, to);

        var result = PatientSyncSnapshotMapper.TryParse(json, out var state);

        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be(PatientSyncErrorCodes.InvalidPayload);
        result.Message.Should().NotContain("King");
        state.Should().BeNull();
    }

    [Fact]
    public void TryParse_RejectsMalformedJson()
    {
        PatientSyncSnapshotMapper.TryParse("{not json", out _).ErrorCode.Should().Be(PatientSyncErrorCodes.InvalidPayload);
    }

    // A different aggregate that shares the replica's id (as the cloud copy would).
    private static string NewPatientWithIdOf(Patient replica)
    {
        var snapshot = PatientSyncSnapshotMapper.ToSnapshot(NewPatient()) with { Id = replica.Id };
        return PatientSyncSnapshotMapper.Serialize(snapshot);
    }

    private static PatientReplicaState ParseState(string json)
    {
        PatientSyncSnapshotMapper.TryParse(json, out var state).Succeeded.Should().BeTrue();
        return state!;
    }
}
