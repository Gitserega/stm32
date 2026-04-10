using System.Threading.Tasks;

namespace Diploma.Application.Services
{
    public class AuthService
    {
        public async Task<bool> IsAuthenticatedAsync()
            => !string.IsNullOrEmpty(await TokenStorage.GetTokenAsync());

        public async Task LogoutAsync()
            => await TokenStorage.RemoveTokenAsync();
    }
}