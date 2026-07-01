using WUnicom.Common.Models;

namespace WUnicom.Common.Abstractions;

public interface IUnifiedCommunicationProvider
{
    string ProviderId { get; }

    ValueTask<ProviderSetupDefinition> GetConnectionSetupDefinitionAsync(
        CancellationToken cancellationToken = default);

    ValueTask<ILoginResult> LoginAsync(
        IAuthenticationRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<ILoginResult> CompleteLoginAsync(
        ILoginContinuationRequest request,
        CancellationToken cancellationToken = default);

    ValueTask LogoutAsync(
        IProviderSession session,
        CancellationToken cancellationToken = default);

    ValueTask<IPaginatedResult<IUnifiedRoom>> SearchRoomsAsync(
        IProviderSession session,
        RoomSearchRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<IPaginatedResult<IUnifiedMessage>> SearchMessagesAsync(
        IProviderSession session,
        MessageSearchRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<IUnifiedMessage> SendMessageAsync(
        IProviderSession session,
        SendPlainTextMessageRequest request,
        CancellationToken cancellationToken = default);
}
