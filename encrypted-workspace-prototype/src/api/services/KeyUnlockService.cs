using Microsoft.EntityFrameworkCore;
using NodaTime;
using encryptedworkspace_api.Data;
using encryptedworkspace_api.Models;
using encryptedworkspace_api.Scopes;

namespace encryptedworkspace_api.Services;

public sealed class KeyUnlockService(
    EncryptedWorkspaceDbContext dbContext,
    AuthorizationService authorizationService,
    ProjectAccessService projectAccessService
)
{
    private const string ActiveStatus = "active";

    public async Task<
        IReadOnlyList<ClientWrappedKeyDto>
    > ListCurrentUserWrappedKeysAsync(
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var context = await authorizationService.RequireAnySystemScopeAsync(
            httpContext,
            [SystemScopeRegistry.UserKeys, SystemScopeRegistry.UserKeysRead],
            ct
        );

        var wrappedKeys = await dbContext
            .ClientWrappedKeys.Where(wrap =>
                wrap.RecipientCryptoPrincipalId
                    == context.User.CryptoPrincipalId
                && wrap.Status == ActiveStatus
            )
            .OrderByDescending(wrap => wrap.CreatedAt)
            .ToArrayAsync(ct);

        return wrappedKeys.Select(ToDto).ToArray();
    }

    public async Task<ProjectKeyUnlockDto> GetProjectKeyUnlockAsync(
        HttpContext httpContext,
        Guid projectId,
        CancellationToken ct
    )
    {
        var context = await projectAccessService.RequireProjectAccessAsync(
            httpContext,
            projectId,
            ProjectAccessService.ProjectReadScope,
            ct
        );
        var activeProjectKey =
            await dbContext
                .CryptoKeys.Where(key =>
                    key.CryptoPrincipalId == context.Project.CryptoPrincipalId
                )
                .OrderByDescending(key => key.CreatedAt)
                .FirstOrDefaultAsync(ct)
            ?? throw new AuthServiceException(
                "Project active public key is not configured.",
                StatusCodes.Status409Conflict
            );

        var accessibleRecipientPrincipalIds =
            context.AccessibleCryptoPrincipalIds.ToArray();
        var currentUserActiveCryptoIds = await dbContext
            .CryptoKeys.Where(key =>
                key.CryptoPrincipalId == context.User.CryptoPrincipalId
            )
            .Select(key => key.CryptoId)
            .ToArrayAsync(ct);

        var wrappedKeyRows = await dbContext
            .ClientWrappedKeys.Where(wrap =>
                wrap.CryptoId == activeProjectKey.CryptoId
                && wrap.Status == ActiveStatus
                && (
                    accessibleRecipientPrincipalIds.Contains(
                        wrap.RecipientCryptoPrincipalId
                    )
                    || (
                        wrap.RecipientCryptoId != null
                        && currentUserActiveCryptoIds.Contains(
                            wrap.RecipientCryptoId.Value
                        )
                    )
                )
            )
            .OrderByDescending(wrap => wrap.CreatedAt)
            .ToArrayAsync(ct);

        return new ProjectKeyUnlockDto(
            context.Project.Id,
            CryptoKeyDtoMapper.ToPublicDto(activeProjectKey),
            wrappedKeyRows.Select(ToDto).ToArray()
        );
    }

    private static ClientWrappedKeyDto ToDto(ClientWrappedKey wrap)
    {
        return new ClientWrappedKeyDto(
            wrap.Id,
            wrap.CryptoId,
            wrap.RecipientCryptoPrincipalId,
            wrap.RecipientCryptoId,
            wrap.WrapKind,
            wrap.Status,
            Convert.ToBase64String(wrap.Ciphertext),
            wrap.EnvelopeJson,
            wrap.LocalSecretFingerprint,
            FormatInstant(wrap.CreatedAt)
        );
    }

    private static string FormatInstant(Instant instant)
    {
        return instant.ToString();
    }
}
