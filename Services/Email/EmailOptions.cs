namespace GanaderiaControl.Services.Email
{
    public class EmailOptions
    {
        public string FromName { get; set; } = "Ganadería Control";
        public string FromAddress { get; set; } = "";
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public bool UseSsl { get; set; } = false;
        public bool UseStartTls { get; set; } = true;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";

        public string? DefaultTo { get; set; } // <- opcional
    }
}
