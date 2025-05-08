namespace SimpleServer.Core.Networking
{
    public record LoginProcessResult(
        bool Success,
        bool RePrompt,
        bool Rejected = false,
        string? Message = null,
        LoginState? State = null,
        string? Username = null
    );
}
