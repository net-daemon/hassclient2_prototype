namespace NetDaemon.Client.Common.Exceptions;

public class HomeAssistantConnectionException : ApplicationException
{
    public DisconnectReason Reason { get; set; }
    public HomeAssistantConnectionException() : base()
    {
    }

    public HomeAssistantConnectionException(string? message) : base(message)
    {
    }

    public HomeAssistantConnectionException(DisconnectReason _reason) : base($"Home assistant disconnected reason:{_reason}")
    {
    }

    public HomeAssistantConnectionException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}