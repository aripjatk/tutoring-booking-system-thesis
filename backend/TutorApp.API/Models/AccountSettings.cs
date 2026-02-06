using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TutorApp.API.Models
{
    public class AccountSettings
    {
        [Key, ForeignKey("Account")]
        public string AccountUsername { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public string ActivationToken { get; set; }
        public DateTime TokenExpirationDate { get; set; }
        public string ProfilePictureFileName { get; set; }

        public Account Account { get; set; }
    }
}
