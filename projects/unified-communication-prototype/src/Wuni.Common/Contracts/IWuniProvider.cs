using Wuni.Common.Contracts.Events;
using Wuni.Common.Contracts.Realms;
using Wuni.Common.Contracts.Rooms;

namespace Wuni.Common.Contracts;

public interface IWuniProvider
{
    public ProviderCapabilities Capabilities { get; }
    
    public Task LoadPersistentData(CancellationToken ct);
    public Task SavePersistentData(CancellationToken ct);

    public IRealmService RealmService { get; }
    public IRoomService RoomService { get; }
    public IEventService EventService { get; }
}