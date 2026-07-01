using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using encryptedworkspace_api.Data;
using encryptedworkspace_api.Models;

namespace encryptedworkspace_api.Services;

public sealed class ProjectMessagingService(
    EncryptedWorkspaceDbContext dbContext,
    IClock clock,
    ProjectAccessService projectAccessService
)
{
    private const string ActiveStatus = "active";
    private const string MainChatKind = "main";
    private const string DirectChatKind = "direct";
    private const string GroupChatKind = "group";
    private const string MainConversationKey = "main";
    private const string MemberParticipantRole = "member";
    private const string ChatMessageRecordType = "project.chat_message.v1";
    private const int MaximumTitleLength = 200;
    private const int MaximumJsonLength = 20_000;
    private const int MaximumMessagePayloadBytes = 256 * 1024;
    private const int DefaultMessageLimit = 100;
    private const int MaximumMessageLimit = 500;

    public async Task<IReadOnlyList<ProjectMemberDto>> ListProjectMembersAsync(
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

        return (
            await ListProjectMemberRowsForProjectAsync(context.Project.Id, ct)
        )
            .Select(ToProjectMemberDto)
            .ToArray();
    }

    public async Task<IReadOnlyList<ProjectChatDto>> ListProjectChatsAsync(
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

        await EnsureMainChatAsync(context, ct);

        return await ListVisibleProjectChatsAsync(context, ct);
    }

    public async Task<ProjectChatDto> CreateProjectChatAsync(
        HttpContext httpContext,
        Guid projectId,
        CreateProjectChatCommand command,
        CancellationToken ct
    )
    {
        var context = await projectAccessService.RequireProjectAccessAsync(
            httpContext,
            projectId,
            ProjectAccessService.ProjectWriteScope,
            ct
        );
        var chatKind = NormalizeRequired(
                command.ChatKind,
                "Chat type is required.",
                maximumLength: 50
            )
            .ToLowerInvariant();

        return chatKind switch
        {
            DirectChatKind => await CreateDirectChatAsync(
                context,
                command.ParticipantUserIds,
                ct
            ),
            GroupChatKind => await CreateGroupChatAsync(context, command, ct),
            MainChatKind => throw new AuthServiceException(
                "The main project chat is created automatically.",
                StatusCodes.Status400BadRequest
            ),
            _ => throw new AuthServiceException(
                "Chat type must be direct or group.",
                StatusCodes.Status400BadRequest
            ),
        };
    }

    public async Task<
        IReadOnlyList<ProjectChatMessageDto>
    > ListProjectChatMessagesAsync(
        HttpContext httpContext,
        Guid projectId,
        Guid chatId,
        long? afterProjectSequence,
        int? limit,
        CancellationToken ct
    )
    {
        var context = await projectAccessService.RequireProjectAccessAsync(
            httpContext,
            projectId,
            ProjectAccessService.ProjectReadScope,
            ct
        );

        _ = await RequireVisibleChatAsync(context, chatId, ct);

        var normalizedAfterSequence = afterProjectSequence.GetValueOrDefault();

        if (normalizedAfterSequence < 0)
        {
            throw new AuthServiceException(
                "After project sequence must be zero or greater.",
                StatusCodes.Status400BadRequest
            );
        }

        var normalizedLimit = NormalizeLimit(limit);
        var rows = await (
            from message in dbContext.ProjectChatMessages
            join sender in dbContext.Users
                on message.SenderUserId equals sender.Id
            where
                message.ProjectId == context.Project.Id
                && message.ProjectChatId == chatId
                && message.DeletedAt == null
                && message.ProjectSequence > normalizedAfterSequence
            orderby message.ProjectSequence
            select new ProjectChatMessageRow(
                message,
                sender.Email,
                sender.DisplayName
            )
        )
            .Take(normalizedLimit)
            .ToArrayAsync(ct);

        return rows.Select(ToProjectChatMessageDto).ToArray();
    }

    public async Task<ProjectChatMessageDto> SendProjectChatMessageAsync(
        HttpContext httpContext,
        Guid projectId,
        Guid chatId,
        EncryptedProjectPayloadUploadDto? payload,
        CancellationToken ct
    )
    {
        var context = await projectAccessService.RequireProjectAccessAsync(
            httpContext,
            projectId,
            ProjectAccessService.ProjectWriteScope,
            ct
        );
        var chat = await RequireVisibleChatAsync(context, chatId, ct);
        var encryptedPayload = await ValidateEncryptedPayloadAsync(
            context,
            payload,
            ct
        );
        var now = clock.GetCurrentInstant();
        var projectSequence = await GetNextProjectSequenceAsync(
            context.Project.Id,
            ct
        );
        var message = new ProjectChatMessage
        {
            Id = Guid.NewGuid(),
            ProjectId = context.Project.Id,
            ProjectChatId = chat.Id,
            RecordType = ChatMessageRecordType,
            ProjectSequence = projectSequence,
            RecordVersion = 1,
            CryptoId = encryptedPayload.CryptoId,
            SchemaVersion = encryptedPayload.SchemaVersion,
            RecordEnvelopeJson = encryptedPayload.RecordEnvelopeJson,
            PayloadEncryptionJson = encryptedPayload.PayloadEncryptionJson,
            PayloadContentHash = encryptedPayload.PayloadContentHash,
            PayloadByteLength = encryptedPayload.PayloadCiphertext.LongLength,
            PayloadCiphertext = encryptedPayload.PayloadCiphertext,
            SenderUserId = context.User.Id,
            SentAt = now,
            CreatedByUserId = context.User.Id,
            CreatedAt = now,
            UpdatedByUserId = context.User.Id,
            UpdatedAt = now,
        };

        chat.LastMessageAt = now;
        dbContext.ProjectChatMessages.Add(message);

        await dbContext.SaveChangesAsync(ct);

        return ToProjectChatMessageDto(
            new ProjectChatMessageRow(
                message,
                context.User.Email,
                context.User.DisplayName
            )
        );
    }

    private async Task<ProjectChatDto> CreateDirectChatAsync(
        ProjectAccessContext context,
        IReadOnlyList<Guid>? participantUserIds,
        CancellationToken ct
    )
    {
        var otherParticipantIds = NormalizeParticipantIds(participantUserIds)
            .Where(userId => userId != context.User.Id)
            .ToArray();

        if (otherParticipantIds.Length != 1)
        {
            throw new AuthServiceException(
                "Direct chats require exactly one other project member.",
                StatusCodes.Status400BadRequest
            );
        }

        var projectMemberIds = await ListProjectMemberIdsForProjectAsync(
            context.Project.Id,
            ct
        );
        var otherParticipantId = otherParticipantIds[0];

        EnsureProjectMember(projectMemberIds, otherParticipantId);

        var directConversationKey = CreateDirectConversationKey(
            context.User.Id,
            otherParticipantId
        );
        var existingChat = await dbContext.ProjectChats.FirstOrDefaultAsync(
            chat =>
                chat.ProjectId == context.Project.Id
                && chat.DirectConversationKey == directConversationKey
                && chat.DeletedAt == null,
            ct
        );

        if (existingChat is not null)
        {
            return await LoadProjectChatDtoAsync(context, existingChat.Id, ct);
        }

        var now = clock.GetCurrentInstant();
        var chat = new ProjectChat
        {
            Id = Guid.NewGuid(),
            ProjectId = context.Project.Id,
            ChatKind = DirectChatKind,
            DirectConversationKey = directConversationKey,
            CreatedByUserId = context.User.Id,
            CreatedAt = now,
        };

        dbContext.ProjectChats.Add(chat);
        AddParticipant(chat.Id, context.Project.Id, context.User.Id, now);
        AddParticipant(chat.Id, context.Project.Id, otherParticipantId, now);

        await dbContext.SaveChangesAsync(ct);

        return await LoadProjectChatDtoAsync(context, chat.Id, ct);
    }

    private async Task<ProjectChatDto> CreateGroupChatAsync(
        ProjectAccessContext context,
        CreateProjectChatCommand command,
        CancellationToken ct
    )
    {
        var title = NormalizeRequired(
            command.Title,
            "Group chat title is required.",
            MaximumTitleLength
        );
        var participantIds = NormalizeParticipantIds(command.ParticipantUserIds)
            .Append(context.User.Id)
            .Distinct()
            .ToArray();

        if (participantIds.Length < 3)
        {
            throw new AuthServiceException(
                "Group chats require at least three project members.",
                StatusCodes.Status400BadRequest
            );
        }

        var projectMemberIds = await ListProjectMemberIdsForProjectAsync(
            context.Project.Id,
            ct
        );

        foreach (var participantId in participantIds)
        {
            EnsureProjectMember(projectMemberIds, participantId);
        }

        var now = clock.GetCurrentInstant();
        var chat = new ProjectChat
        {
            Id = Guid.NewGuid(),
            ProjectId = context.Project.Id,
            ChatKind = GroupChatKind,
            Title = title,
            CreatedByUserId = context.User.Id,
            CreatedAt = now,
        };

        dbContext.ProjectChats.Add(chat);

        foreach (var participantId in participantIds)
        {
            AddParticipant(chat.Id, context.Project.Id, participantId, now);
        }

        await dbContext.SaveChangesAsync(ct);

        return await LoadProjectChatDtoAsync(context, chat.Id, ct);
    }

    private async Task<
        IReadOnlyList<ProjectChatDto>
    > ListVisibleProjectChatsAsync(
        ProjectAccessContext context,
        CancellationToken ct
    )
    {
        var participantChatIds = dbContext
            .ProjectChatParticipants.Where(participant =>
                participant.ProjectId == context.Project.Id
                && participant.UserId == context.User.Id
                && participant.RemovedAt == null
            )
            .Select(participant => participant.ProjectChatId);
        var chats = await dbContext
            .ProjectChats.Where(chat =>
                chat.ProjectId == context.Project.Id
                && chat.DeletedAt == null
                && (
                    chat.ChatKind == MainChatKind
                    || participantChatIds.Contains(chat.Id)
                )
            )
            .ToArrayAsync(ct);
        var chatIds = chats.Select(chat => chat.Id).ToArray();
        var participantRows = await ListParticipantRowsAsync(chatIds, ct);
        var participantsByChatId = participantRows
            .GroupBy(row => row.ProjectChatId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(ToProjectChatParticipantDto).ToArray()
            );

        return chats
            .Select(chat =>
                ToProjectChatDto(
                    chat,
                    participantsByChatId.GetValueOrDefault(chat.Id) ?? []
                )
            )
            .OrderBy(chat =>
                string.Equals(
                    chat.ChatKind,
                    MainChatKind,
                    StringComparison.Ordinal
                )
                    ? 0
                    : 1
            )
            .ThenByDescending(chat =>
                DateTimeOffset.Parse(chat.LastMessageAt ?? chat.CreatedAt)
            )
            .ThenBy(
                chat => chat.Title ?? string.Empty,
                StringComparer.OrdinalIgnoreCase
            )
            .ToArray();
    }

    private async Task<ProjectChatDto> LoadProjectChatDtoAsync(
        ProjectAccessContext context,
        Guid chatId,
        CancellationToken ct
    )
    {
        var chat = await RequireVisibleChatAsync(context, chatId, ct);
        var participants = (await ListParticipantRowsAsync([chat.Id], ct))
            .Select(ToProjectChatParticipantDto)
            .ToArray();

        return ToProjectChatDto(chat, participants);
    }

    private async Task<ProjectChat> RequireVisibleChatAsync(
        ProjectAccessContext context,
        Guid chatId,
        CancellationToken ct
    )
    {
        var chat =
            await dbContext.ProjectChats.FirstOrDefaultAsync(
                row =>
                    row.Id == chatId
                    && row.ProjectId == context.Project.Id
                    && row.DeletedAt == null,
                ct
            )
            ?? throw new AuthServiceException(
                "Project chat was not found.",
                StatusCodes.Status404NotFound
            );

        if (
            string.Equals(chat.ChatKind, MainChatKind, StringComparison.Ordinal)
        )
        {
            return chat;
        }

        var isParticipant = await dbContext.ProjectChatParticipants.AnyAsync(
            participant =>
                participant.ProjectChatId == chat.Id
                && participant.UserId == context.User.Id
                && participant.RemovedAt == null,
            ct
        );

        if (!isParticipant)
        {
            throw new AuthServiceException(
                "Project chat participant access is required.",
                StatusCodes.Status403Forbidden
            );
        }

        return chat;
    }

    private async Task EnsureMainChatAsync(
        ProjectAccessContext context,
        CancellationToken ct
    )
    {
        var existingChat = await dbContext.ProjectChats.AnyAsync(
            chat =>
                chat.ProjectId == context.Project.Id
                && chat.DirectConversationKey == MainConversationKey,
            ct
        );

        if (existingChat)
        {
            return;
        }

        var now = clock.GetCurrentInstant();

        dbContext.ProjectChats.Add(
            new ProjectChat
            {
                Id = Guid.NewGuid(),
                ProjectId = context.Project.Id,
                ChatKind = MainChatKind,
                Title = "Main",
                DirectConversationKey = MainConversationKey,
                CreatedByUserId = context.User.Id,
                CreatedAt = now,
            }
        );

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task<ValidatedEncryptedPayload> ValidateEncryptedPayloadAsync(
        ProjectAccessContext context,
        EncryptedProjectPayloadUploadDto? payload,
        CancellationToken ct
    )
    {
        if (payload is null)
        {
            throw new AuthServiceException(
                "Encrypted message payload is required.",
                StatusCodes.Status400BadRequest
            );
        }

        if (payload.CryptoId == Guid.Empty)
        {
            throw new AuthServiceException(
                "Encrypted message crypto id is required.",
                StatusCodes.Status400BadRequest
            );
        }

        if (payload.SchemaVersion < 1)
        {
            throw new AuthServiceException(
                "Encrypted message schema version must be greater than zero.",
                StatusCodes.Status400BadRequest
            );
        }

        var projectKeyExists = await dbContext.CryptoKeys.AnyAsync(
            key =>
                key.CryptoId == payload.CryptoId
                && key.CryptoPrincipalId == context.Project.CryptoPrincipalId,
            ct
        );

        if (!projectKeyExists)
        {
            throw new AuthServiceException(
                "Encrypted message crypto id does not belong to this project.",
                StatusCodes.Status400BadRequest
            );
        }

        var ciphertext = DecodeRequiredBase64(
            payload.PayloadCiphertextBase64,
            "Encrypted message ciphertext is required."
        );

        if (payload.PayloadByteLength != ciphertext.LongLength)
        {
            throw new AuthServiceException(
                "Encrypted message payload length does not match the ciphertext.",
                StatusCodes.Status400BadRequest
            );
        }

        if (ciphertext.LongLength > MaximumMessagePayloadBytes)
        {
            throw new AuthServiceException(
                $"Encrypted message payload must be {MaximumMessagePayloadBytes} bytes or less.",
                StatusCodes.Status400BadRequest
            );
        }

        var payloadContentHash = NormalizeRequired(
            payload.PayloadContentHash,
            "Encrypted message payload hash is required.",
            maximumLength: 200
        );
        var actualHash = ComputePayloadContentHash(ciphertext);

        if (
            !string.Equals(
                payloadContentHash,
                actualHash,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new AuthServiceException(
                "Encrypted message payload hash does not match the ciphertext.",
                StatusCodes.Status400BadRequest
            );
        }

        return new ValidatedEncryptedPayload(
            payload.CryptoId,
            payload.SchemaVersion,
            NormalizeJson(
                payload.RecordEnvelopeJson,
                "Encrypted message record envelope"
            ),
            NormalizeJson(
                payload.PayloadEncryptionJson,
                "Encrypted message encryption metadata"
            ),
            actualHash,
            ciphertext
        );
    }

    private async Task<long> GetNextProjectSequenceAsync(
        Guid projectId,
        CancellationToken ct
    )
    {
        var previousSequence =
            await dbContext
                .ProjectChatMessages.Where(message =>
                    message.ProjectId == projectId
                )
                .MaxAsync(message => (long?)message.ProjectSequence, ct)
            ?? 0;

        return previousSequence + 1;
    }

    private async Task<IReadOnlySet<Guid>> ListProjectMemberIdsForProjectAsync(
        Guid projectId,
        CancellationToken ct
    )
    {
        return (await ListProjectMemberRowsForProjectAsync(projectId, ct))
            .Select(row => row.UserId)
            .ToHashSet();
    }

    private async Task<
        IReadOnlyList<ProjectMemberRow>
    > ListProjectMemberRowsForProjectAsync(Guid projectId, CancellationToken ct)
    {
        var directRows = await (
            from grant in dbContext.ProjectPrincipalGrants
            join user in dbContext.Users
                on grant.CryptoPrincipalId equals user.CryptoPrincipalId
            where
                grant.ProjectId == projectId
                && grant.Status == ActiveStatus
                && user.Status == ActiveStatus
            select new ProjectMemberRow(user.Id, user.Email, user.DisplayName)
        ).ToArrayAsync(ct);
        var organizationRows = await (
            from grant in dbContext.ProjectPrincipalGrants
            join organization in dbContext.Organizations
                on grant.CryptoPrincipalId equals organization.CryptoPrincipalId
            join membership in dbContext.OrganizationMemberships
                on organization.Id equals membership.OrganizationId
            join user in dbContext.Users on membership.UserId equals user.Id
            where
                grant.ProjectId == projectId
                && grant.Status == ActiveStatus
                && organization.DeletedAt == null
                && membership.Status == ActiveStatus
                && user.Status == ActiveStatus
            select new ProjectMemberRow(user.Id, user.Email, user.DisplayName)
        ).ToArrayAsync(ct);
        var organizationGroupRows = await (
            from grant in dbContext.ProjectPrincipalGrants
            join organizationGroup in dbContext.OrganizationGroups
                on grant.CryptoPrincipalId equals organizationGroup.CryptoPrincipalId
            join membership in dbContext.OrganizationGroupMemberships
                on organizationGroup.Id equals membership.OrganizationGroupId
            join user in dbContext.Users on membership.UserId equals user.Id
            where
                grant.ProjectId == projectId
                && grant.Status == ActiveStatus
                && organizationGroup.DeletedAt == null
                && membership.Status == ActiveStatus
                && user.Status == ActiveStatus
            select new ProjectMemberRow(user.Id, user.Email, user.DisplayName)
        ).ToArrayAsync(ct);

        return directRows
            .Concat(organizationRows)
            .Concat(organizationGroupRows)
            .GroupBy(row => row.UserId)
            .Select(group => group.First())
            .OrderBy(
                row => row.DisplayName ?? row.Email,
                StringComparer.OrdinalIgnoreCase
            )
            .ToArray();
    }

    private async Task<
        IReadOnlyList<ProjectChatParticipantRow>
    > ListParticipantRowsAsync(
        IReadOnlyList<Guid> chatIds,
        CancellationToken ct
    )
    {
        if (chatIds.Count == 0)
        {
            return [];
        }

        return await (
            from participant in dbContext.ProjectChatParticipants
            join user in dbContext.Users on participant.UserId equals user.Id
            where
                chatIds.Contains(participant.ProjectChatId)
                && participant.RemovedAt == null
            orderby user.DisplayName ?? user.Email
            select new ProjectChatParticipantRow(
                participant.ProjectChatId,
                user.Id,
                user.Email,
                user.DisplayName,
                participant.ParticipantRole
            )
        ).ToArrayAsync(ct);
    }

    private void AddParticipant(
        Guid chatId,
        Guid projectId,
        Guid userId,
        Instant now
    )
    {
        dbContext.ProjectChatParticipants.Add(
            new ProjectChatParticipant
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                ProjectChatId = chatId,
                UserId = userId,
                ParticipantRole = MemberParticipantRole,
                JoinedAt = now,
            }
        );
    }

    private static void EnsureProjectMember(
        IReadOnlySet<Guid> projectMemberIds,
        Guid userId
    )
    {
        if (!projectMemberIds.Contains(userId))
        {
            throw new AuthServiceException(
                "All chat participants must be active project members.",
                StatusCodes.Status400BadRequest
            );
        }
    }

    private static string CreateDirectConversationKey(Guid left, Guid right)
    {
        var orderedIds = new[] { left, right }
            .OrderBy(id => id)
            .Select(id => id.ToString("N"));

        return $"direct:{string.Join(":", orderedIds)}";
    }

    private static IReadOnlyList<Guid> NormalizeParticipantIds(
        IReadOnlyList<Guid>? participantUserIds
    )
    {
        return (participantUserIds ?? [])
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToArray();
    }

    private static int NormalizeLimit(int? limit)
    {
        if (limit is null)
        {
            return DefaultMessageLimit;
        }

        if (limit is < 1 or > MaximumMessageLimit)
        {
            throw new AuthServiceException(
                $"Message limit must be between 1 and {MaximumMessageLimit}.",
                StatusCodes.Status400BadRequest
            );
        }

        return limit.Value;
    }

    private static string NormalizeRequired(
        string? value,
        string message,
        int maximumLength
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new AuthServiceException(
                message,
                StatusCodes.Status400BadRequest
            );
        }

        var normalized = value.Trim();

        if (normalized.Length > maximumLength)
        {
            throw new AuthServiceException(
                $"{message.TrimEnd('.')} must be {maximumLength} characters or less.",
                StatusCodes.Status400BadRequest
            );
        }

        return normalized;
    }

    private static string NormalizeJson(string? value, string label)
    {
        var json = NormalizeRequired(
            value,
            $"{label} is required.",
            MaximumJsonLength
        );

        try
        {
            using var _ = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new AuthServiceException(
                $"{label} must be valid JSON: {exception.Message}",
                StatusCodes.Status400BadRequest
            );
        }

        return json;
    }

    private static byte[] DecodeRequiredBase64(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new AuthServiceException(
                message,
                StatusCodes.Status400BadRequest
            );
        }

        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException exception)
        {
            throw new AuthServiceException(
                $"{message} It must be base64: {exception.Message}",
                StatusCodes.Status400BadRequest
            );
        }
    }

    private static string ComputePayloadContentHash(byte[] ciphertext)
    {
        var hash = SHA256.HashData(ciphertext);

        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static ProjectMemberDto ToProjectMemberDto(ProjectMemberRow row)
    {
        return new ProjectMemberDto(row.UserId, row.Email, row.DisplayName);
    }

    private static ProjectChatParticipantDto ToProjectChatParticipantDto(
        ProjectChatParticipantRow row
    )
    {
        return new ProjectChatParticipantDto(
            row.UserId,
            row.Email,
            row.DisplayName,
            row.ParticipantRole
        );
    }

    private static ProjectChatDto ToProjectChatDto(
        ProjectChat chat,
        IReadOnlyList<ProjectChatParticipantDto> participants
    )
    {
        return new ProjectChatDto(
            chat.Id,
            chat.ProjectId,
            chat.ChatKind,
            chat.Title,
            participants,
            FormatInstant(chat.CreatedAt),
            chat.LastMessageAt is null
                ? null
                : FormatInstant(chat.LastMessageAt.Value)
        );
    }

    private static ProjectChatMessageDto ToProjectChatMessageDto(
        ProjectChatMessageRow row
    )
    {
        return new ProjectChatMessageDto(
            row.Message.Id,
            row.Message.ProjectId,
            row.Message.ProjectChatId,
            row.Message.SenderUserId,
            row.SenderEmail,
            row.SenderDisplayName,
            row.Message.ProjectSequence,
            row.Message.RecordVersion,
            row.Message.CryptoId,
            row.Message.SchemaVersion,
            row.Message.RecordEnvelopeJson,
            row.Message.PayloadEncryptionJson,
            row.Message.PayloadContentHash,
            row.Message.PayloadByteLength,
            Convert.ToBase64String(row.Message.PayloadCiphertext),
            FormatInstant(row.Message.SentAt)
        );
    }

    private static string FormatInstant(Instant instant)
    {
        return instant.ToString();
    }

    public sealed record CreateProjectChatCommand(
        string? ChatKind,
        string? Title,
        IReadOnlyList<Guid>? ParticipantUserIds
    );

    private sealed record ProjectMemberRow(
        Guid UserId,
        string Email,
        string? DisplayName
    );

    private sealed record ProjectChatParticipantRow(
        Guid ProjectChatId,
        Guid UserId,
        string Email,
        string? DisplayName,
        string ParticipantRole
    );

    private sealed record ProjectChatMessageRow(
        ProjectChatMessage Message,
        string SenderEmail,
        string? SenderDisplayName
    );

    private sealed record ValidatedEncryptedPayload(
        Guid CryptoId,
        int SchemaVersion,
        string RecordEnvelopeJson,
        string PayloadEncryptionJson,
        string PayloadContentHash,
        byte[] PayloadCiphertext
    );
}
