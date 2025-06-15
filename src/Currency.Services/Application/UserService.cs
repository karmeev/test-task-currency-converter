using Currency.Data.Contracts;
using Currency.Data.Contracts.Exceptions;
using Currency.Domain.Login;
using Currency.Domain.Users;
using Currency.Infrastructure.Contracts.Auth;
using Currency.Services.Contracts.Application;
using Microsoft.Extensions.Logging;

namespace Currency.Services.Application;

internal class UserService(
    ILogger<UserService> logger,
    ISecretHasher secretHasher,
    IUsersRepository usersRepository) : IUserService
{
    public async Task<User> GetUserAsync(LoginModel model, CancellationToken token)
    {
        try
        {
            var user = await usersRepository.GetUserByUsernameAsync(model, token);
            return secretHasher.Verify(model.Password, user.Password) ? user : null;
        }
        catch (NotFoundException ex)
        {
            logger.LogError(ex, "User {username} not found", model.Username);
            throw;
        }
    }

    #nullable enable
    public async Task<User?> TryGetUserByIdAsync(string userId, CancellationToken token)
    {
        return await usersRepository.GetUserByIdAsync(userId, token);
    }
}