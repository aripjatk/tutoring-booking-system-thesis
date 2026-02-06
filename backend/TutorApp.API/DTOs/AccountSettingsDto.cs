namespace TutorApp.API.DTOs
{
    public class AccountSettingsDto
    {
        public string AccountUsername { get; set; }
        public DateTime TokenExpirationDate { get; set; }
        public string ProfilePictureFileName { get; set; }
    }
}
