using TutorApp.API.Models;

namespace TutorApp.API.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(Account account);
    }
}
