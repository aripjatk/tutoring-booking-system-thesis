namespace TutorApp.API.DTOs
{
    public class AccountDto
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public bool IsTutor { get; set; }
        public bool IsActive { get; set; }
    }
}
