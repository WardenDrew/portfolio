using CaptivePortal.Database;
using CaptivePortal.Database.Entities;
using CaptivePortal.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;

namespace CaptivePortal.Services.Outer
{
    public class WebAuthenticationService
    {
        private readonly IDbContextFactory<IronNacDbContext> dbFactory;
        private static readonly Sodium.PasswordHash.StrengthArgon strength = Sodium.PasswordHash.StrengthArgon.Interactive;

        public WebAuthenticationService(IDbContextFactory<IronNacDbContext> dbFactory)
        {
            this.dbFactory = dbFactory;
        }

        public async Task<bool> ValidateLoginAsync(
            string? email,
            string? password,
            CancellationToken cancellationToken = default)
        {
            if (email is null || password is null) return false;
            email = email.ToLowerInvariant();

            IronNacDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

            string? hash = await db.Users
                .AsNoTracking()
                .Where(x => x.Email == email)
                .Select(x => x.Hash)
                .FirstOrDefaultAsync(cancellationToken);

            if (hash is null) return false;

            return Sodium.PasswordHash.ArgonHashStringVerify(hash, password);
        }

        public async Task<bool> ChangePasswordAsync(
            string? email,
            string? oldPassword,
            string? newPassword,
            bool changePasswordNextLogin = false,
            CancellationToken cancellationToken = default)
        {
            if (email is null || oldPassword is null || newPassword is null) return false;
            email = email.ToLowerInvariant();

            IronNacDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

            User? user = await db.Users
                .AsNoTracking()
                .Where(x => x.Email == email)
                .FirstOrDefaultAsync(cancellationToken);
            if (user is null) return false;

            if (!Sodium.PasswordHash.ArgonHashStringVerify(user.Hash, oldPassword)) return false;

            return await SetPasswordAsync(email, newPassword, changePasswordNextLogin, cancellationToken);
        }

        public async Task<bool> SetPasswordAsync(
            string? email,
            string? password,
            bool changePasswordNextLogin = false,
            CancellationToken cancellationToken = default)
        {
            if (email is null || password is null) return false;
            email = email.ToLowerInvariant();

            string newHash = GetHash(password);

            IronNacDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

            int modified = await db.Users
                .Where(x => x.Email == email)
                .ExecuteUpdateAsync(x => x
                    .SetProperty(p => p.Hash, newHash)
                    .SetProperty(p => p.ChangePasswordNextLogin, changePasswordNextLogin)
                , cancellationToken);

            return modified > 0; // TODO error reporting if modifying more than 1
        }

        public static string GetHash(string password)
        {
            return Sodium.PasswordHash.ArgonHashString(password, strength);
        }

        public async Task<WebLoginResult> WebLoginAsync(ProtectedLocalStorage protectedLocalStorage, string? email, string? password, CancellationToken cancellationToken = default)
        {
            WebLoginResult result = new();

            result.Success = await ValidateLoginAsync(email, password);
            if (!result.Success) return result;
            email = email?.ToLowerInvariant();

            IronNacDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

            User? user = await db.Users
                .AsNoTracking()
                .Where(x => x.Email == email)
                .FirstOrDefaultAsync();
            if (user is null) return result;

            result.AccessToken = new()
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                PermissionLevel = user.PermissionLevel,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                RefreshToken = Guid.NewGuid(),
                RefreshTokenIssuedAt = DateTime.UtcNow,
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            UserSession session = new()
            {
                UserId = user.Id,
                RefreshToken = result.AccessToken.RefreshToken,
                RefreshTokenIssuedAt = result.AccessToken.RefreshTokenIssuedAt,
                RefreshTokenExpiresAt = result.AccessToken.RefreshTokenExpiresAt
            };

            db.Add(session);
            await db.SaveChangesAsync(cancellationToken);

            await protectedLocalStorage.SetAsync(nameof(AccessToken), result.AccessToken);

            if (user.ChangePasswordNextLogin)
            {
                result.ChangePasswordRequired = true;
                return result;
            }

            return result;
        }

        public async Task<AccessToken?> WebCheckLoggedInAsync(ProtectedLocalStorage protectedLocalStorage, CancellationToken cancellationToken = default)
        {
            ProtectedBrowserStorageResult<AccessToken> plsResult;
            try
            {
                plsResult = await protectedLocalStorage.GetAsync<AccessToken>(nameof(AccessToken));
            }
            catch (Exception)
            {
                try
                {
                    await protectedLocalStorage.DeleteAsync(nameof(AccessToken));
                }
                catch (Exception) { }
                return null;
            }

            if (!plsResult.Success) return null;

            AccessToken? accessToken = plsResult.Value;
            if (accessToken is null) return null;

            if (accessToken.IssuedAt > DateTime.UtcNow ||
                accessToken.ExpiresAt <= DateTime.UtcNow)
            {
                accessToken = await ExchangeRefreshTokenAsync(protectedLocalStorage, accessToken, cancellationToken);
            }

            return accessToken;
        }

        public async Task WebLogoutAsync(ProtectedLocalStorage protectedLocalStorage, CancellationToken cancellationToken = default)
        {
            await protectedLocalStorage.DeleteAsync(nameof(AccessToken));
        }

        public async Task<AccessToken?> ExchangeRefreshTokenAsync(ProtectedLocalStorage protectedLocalStorage, AccessToken oldAccessToken, CancellationToken cancellationToken = default)
        {
            if (oldAccessToken.RefreshTokenIssuedAt > DateTime.UtcNow ||
                oldAccessToken.RefreshTokenExpiresAt <= DateTime.UtcNow)
            {
                return null;
            }

            IronNacDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

            UserSession? userSession = await db.UserSessions
                .Include(x => x.User)
                .Where(x => x.UserId == oldAccessToken.UserId)
                .Where(x => x.RefreshToken == oldAccessToken.RefreshToken)
                .FirstOrDefaultAsync(cancellationToken);
            if (userSession is null) return null;

            if (userSession.RefreshTokenIssuedAt > DateTime.UtcNow ||
                userSession.RefreshTokenExpiresAt <= DateTime.UtcNow)
            {
                return null;
            }

            AccessToken newAccessToken = new()
            {
                UserId = userSession.UserId,
                Name = userSession.User.Name,
                Email = userSession.User.Email,
                PermissionLevel = userSession.User.PermissionLevel,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                RefreshToken = userSession.RefreshToken,
                RefreshTokenIssuedAt = userSession.RefreshTokenIssuedAt,
                RefreshTokenExpiresAt = userSession.RefreshTokenExpiresAt
            };

            await protectedLocalStorage.SetAsync(nameof(AccessToken), newAccessToken);

            return newAccessToken;
        }

        public bool CheckComplexity(string? password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            if (password.Length < 8) return false;

            return true;
        }
    }
}
