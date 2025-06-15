using Currency.Data.Contracts;
using Currency.Data.Contracts.Exceptions;
using Currency.Domain.Login;
using Currency.Domain.Users;
using Currency.Infrastructure.Contracts.Databases.Base;
using Currency.Infrastructure.Contracts.Databases.Redis;

namespace Currency.Data.Repositories;

internal class UsersRepository(IRedisContext context) : IUsersRepository
{
    private static string Prefix => EntityPrefix.UserPrefix;
    
    public async Task<User> GetUserByUsernameAsync(LoginModel model, CancellationToken token)
    {
        var index = $"{Prefix}:user-by-username:{model.Username}";
        var user = await context.TryGetByIndexAsync<User>(index);
        if (user is null)
        {
            return NotFoundException.Throw<User>($"User {model.Username} not found");
        }
        return user;
    }

    public async Task<User> GetUserByIdAsync(string id, CancellationToken token)
    {
        var index = $"{Prefix}:user-by-id:{id}";
        var user = await context.TryGetByIndexAsync<User>(index);
        return user;
    }
}