using FluentAssertions;
using HMS.Modules.Documents.Application;
using HMS.Modules.Documents.Application.Security;
using HMS.Modules.Documents.Contracts;
using Xunit;

namespace HMS.UnitTests.Modules.Documents.Application;

/// <summary>ADR-130: an actor scoped to Patient documents (it came in through a
/// patient-management permission) reaches nothing else, whatever its LoginType allows.</summary>
public class DocumentAccessPolicyScopeTests
{
    private static readonly IReadOnlySet<DocumentOwnerType> PatientOnly = new HashSet<DocumentOwnerType> { DocumentOwnerType.Patient };
    private readonly DocumentAccessPolicy _sut = new();

    [Fact]
    public void AnUnscopedReceptionist_KeepsEveryOwnerTypeTheTableAllows()
    {
        var actor = new DocumentActor(Guid.NewGuid(), "receptionist");

        _sut.CanRead(actor, DocumentOwnerType.Patient, DocumentClassification.Internal).Should().BeTrue();
        _sut.CanRead(actor, DocumentOwnerType.Billing, DocumentClassification.Internal).Should().BeTrue();
        _sut.CanWrite(actor, DocumentOwnerType.Appointment).Should().BeTrue();
    }

    [Fact]
    public void AScopedReceptionist_CanReadAndWritePatientDocuments()
    {
        var actor = new DocumentActor(Guid.NewGuid(), "receptionist", PatientOnly);

        _sut.CanRead(actor, DocumentOwnerType.Patient, DocumentClassification.Internal).Should().BeTrue();
        _sut.CanWrite(actor, DocumentOwnerType.Patient).Should().BeTrue();
    }

    [Fact]
    public void AScopedReceptionist_LosesBillingAndAppointmentDocuments()
    {
        var actor = new DocumentActor(Guid.NewGuid(), "receptionist", PatientOnly);

        _sut.CanRead(actor, DocumentOwnerType.Billing, DocumentClassification.Internal).Should().BeFalse();
        _sut.CanWrite(actor, DocumentOwnerType.Appointment).Should().BeFalse();
    }

    [Fact]
    public void TheScope_StillAppliesToABypassRole()
    {
        var actor = new DocumentActor(Guid.NewGuid(), "superAdmin", PatientOnly);

        _sut.CanRead(actor, DocumentOwnerType.Staff, DocumentClassification.Internal).Should().BeFalse();
        _sut.CanRead(actor, DocumentOwnerType.Patient, DocumentClassification.Internal).Should().BeTrue();
    }

    [Fact]
    public void TheScope_NeverWidensTheTable()
    {
        // labTechnician has no Patient access in the LoginType table; a Patient scope doesn't add it.
        var actor = new DocumentActor(Guid.NewGuid(), "labTechnician", PatientOnly);

        _sut.CanRead(actor, DocumentOwnerType.Patient, DocumentClassification.Internal).Should().BeFalse();
    }

    [Fact]
    public void AScopedReceptionist_StillCannotReadRestrictedPatientDocuments()
    {
        var actor = new DocumentActor(Guid.NewGuid(), "receptionist", PatientOnly);

        _sut.CanRead(actor, DocumentOwnerType.Patient, DocumentClassification.Restricted).Should().BeFalse();
    }
}
