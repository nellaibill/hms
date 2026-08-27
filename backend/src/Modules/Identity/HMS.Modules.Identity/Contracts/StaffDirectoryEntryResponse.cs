namespace HMS.Modules.Identity.Contracts;

/// <summary>
/// A deliberately minimal, low-sensitivity view of a user — just enough for a staff-picker
/// (e.g. HMS.Modules.Messaging's "start a conversation" screen) to let any authenticated
/// staff member find a colleague by name. Unlike <see cref="UserResponse"/> (which requires
/// "identity-administration.view", an admin-level permission), this carries no email/phone/
/// login metadata and is available to any authenticated user — see
/// UsersController.GetDirectory's own doc comment for why this exists.
/// </summary>
public record StaffDirectoryEntryResponse
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
}
