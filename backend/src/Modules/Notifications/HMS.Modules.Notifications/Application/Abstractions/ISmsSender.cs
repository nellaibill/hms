namespace HMS.Modules.Notifications.Application.Abstractions;

/// <summary>Implemented in Infrastructure by HttpSmsSender.</summary>
internal interface ISmsSender
{
    Task SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken);
}
