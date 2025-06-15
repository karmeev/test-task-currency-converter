using Currency.Domain.Login;
using Currency.Domain.Users;

namespace Currency.Services.Contracts.Application;

#nullable enable
public interface IUserService
{
    Task<User> GetUserAsync(LoginModel model, CancellationToken token);
    Task<User?> TryGetUserByIdAsync(string userId, CancellationToken token);
}