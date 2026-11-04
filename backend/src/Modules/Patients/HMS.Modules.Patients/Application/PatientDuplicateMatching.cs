namespace HMS.Modules.Patients.Application;

/// <summary>
/// Whether a new registration is a patient already on file (same phone assumed — callers only
/// compare patients sharing a primary phone). Used to refuse the duplicate at registration and
/// bulk import (IPatientRepository.FindDuplicateAsync).
/// <para>
/// Built around the legacy records bulk-imported from the old system: their whole name sits in
/// FirstName with LastName blank ("Jeya Sathya" + ""), and none has an ID number. Comparing first
/// and last name separately, and requiring the ID number to match whenever one was entered (it
/// always is now), meant a returning legacy patient never matched and was registered again
/// under a second UHID.
/// </para>
/// </summary>
internal static class PatientDuplicateMatching
{
    /// <summary>The full name, case- and spacing-insensitive: "Jeya Sathya" + "" and "jeya" +
    /// " Sathya " both become "jeya sathya".</summary>
    public static string FullNameKey(string? firstName, string? lastName)
        => string.Join(' ', $"{firstName} {lastName}".Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    /// <summary>
    /// Same full name, and the ID numbers don't disagree. A blank ID number on either side
    /// can't tell the two apart, so name and phone decide; two different ID numbers mean two
    /// different people (e.g. a father and son with the same name and phone).
    /// </summary>
    public static bool IsSamePatient(
        string existingFirstName,
        string existingLastName,
        string? existingIdProofNumber,
        string firstName,
        string lastName,
        string? idProofNumber)
    {
        if (FullNameKey(existingFirstName, existingLastName) != FullNameKey(firstName, lastName))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(idProofNumber)
            || string.IsNullOrWhiteSpace(existingIdProofNumber)
            || string.Equals(existingIdProofNumber.Trim(), idProofNumber.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
