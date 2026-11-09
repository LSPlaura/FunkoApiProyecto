namespace FunkoApi.Back.Config;

public class SmtpSettings {
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "noreply@tienda.com";
    public string DisplayName { get; set; } = "FunkoApi";
    public string Security { get; set; } = "Auto";
}