using Currency.Data.Contracts.Exceptions;
using Currency.Domain.Login;
using Currency.Facades.Contracts;
using Currency.Facades.Contracts.Requests;
using Currency.Facades.Contracts.Responses;
using Currency.Facades.Validators;
using Currency.Services.Contracts.Application;
using Microsoft.Extensions.Logging;

namespace Currency.Facades;

internal class AuthFacade(
    IUserService userService,
    ITokenService tokenService,
    ILogger<AuthFacade> logger) : IAuthFacade
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        logger.LogInformation("started; start login. User: {name}", request.Username);
        
        var validationResult = AuthValidator.Validate(request.Username, request.Password);
        if (!validationResult.IsValid) return AuthResponse.Error(validationResult.Message);

        var model = new LoginModel(request.Username, request.Password);
        try
        {
            var user = await userService.GetUserAsync(model, ct);
            
            var tokenModel = tokenService.GenerateTokens(user, ct);
            await tokenService.AddRefreshTokenAsync(tokenModel.RefreshToken, user.Id, ct);

            return AuthResponse.Success(tokenModel.AccessToken, tokenModel.RefreshToken,
                tokenModel.ExpiresAt);
        }
        catch (NotFoundException)
        {
            logger.LogWarning("failed; User not found or credentials incorrect. Username: {name}", request.Username);
            return AuthResponse.Error("Incorrect credentials");
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(string token, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        
        if (string.IsNullOrEmpty(token)) return AuthResponse.Error("Invalid refresh token");

        var refreshToken = await tokenService.GetRefreshTokenAsync(token, ct);
        if (!refreshToken.Verified)
        {
            logger.LogWarning("completed; Refresh token is not verified: {token}", token);
            return AuthResponse.Error("Refresh token is not verified");
        }

        var user = await userService.TryGetUserByIdAsync(refreshToken.UserId, ct);
        if (user is null) return AuthResponse.Error("User not found");

        var accessToken = tokenService.GenerateAccessToken(user, ct);

        return AuthResponse.Success(accessToken.Token, token, accessToken.ExpiresAt);
    }
}