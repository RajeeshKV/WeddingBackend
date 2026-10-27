namespace WeddingInvite.Application.Options;

public sealed class BrevoOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.brevo.com/v3/";
}
