using CaptivePortal.Daemons;
using System.Reflection.Metadata.Ecma335;

namespace CaptivePortal.Services.Outer
{
    public class DaemonInteractionService(
        OuterServiceProviderService outerSP)
    {
        private T Get<T>() where T : BaseDaemon<T>
        {
            T? daemon = outerSP.OuterServiceProvider.GetServices<IHostedService>()
                .Where(x => x.GetType().IsAssignableTo(typeof(T)))
                .Cast<T>()
                .FirstOrDefault();

            if (daemon is null)
            {
                throw new KeyNotFoundException();
            }

            return daemon;
        }

        public bool GetRunning<T>() where T : BaseDaemon<T>
            => Get<T>().Running;

        public void Start<T>() where T : BaseDaemon<T>
            => Get<T>().StartDaemon();

        public void Start(Type type)
        {
            if (!type.IsAssignableTo(typeof(BaseDaemon<>).MakeGenericType(type)))
                throw new InvalidOperationException();

            typeof(DaemonInteractionService)
                .GetMethod(nameof(Start), 1, [])?
                .MakeGenericMethod(type)
                .Invoke(this, null);
        }

        public Task<bool> Stop<T>(CancellationToken cancellationToken = default) where T : BaseDaemon<T>
            => Get<T>().StopDaemonAsync(cancellationToken);

        public Task<bool> Stop(Type type, CancellationToken cancellationToken = default)
        {
            if (!type.IsAssignableTo(typeof(BaseDaemon<>).MakeGenericType(type)))
                throw new InvalidOperationException();

            Task<bool>? result = (Task<bool>?)(
                typeof(DaemonInteractionService)
                    .GetMethod(nameof(Stop), 1, [typeof(CancellationToken)])?
                    .MakeGenericMethod(type)
                    .Invoke(this, [cancellationToken]));
            if (result is null)
                throw new InvalidOperationException();

            return result;
        }

        public async Task<bool> Restart<T>(CancellationToken cancellationToken = default) where T : BaseDaemon<T>
        {
            T daemon = Get<T>();

            bool stopped = await daemon.StopDaemonAsync(cancellationToken);
            if (!stopped) return false;

            daemon.StartDaemon();
            return true;
        }

        public Task<bool> Restart(Type type, CancellationToken cancellationToken = default)
        {
            if (!type.IsAssignableTo(typeof(BaseDaemon<>).MakeGenericType(type)))
                throw new InvalidOperationException();

            Task<bool>? result = (Task<bool>?)(
                typeof(DaemonInteractionService)
                    .GetMethod(nameof(Restart), 1, [typeof(CancellationToken)])?
                    .MakeGenericMethod(type)
                    .Invoke(this, [cancellationToken]));
            if (result is null)
                throw new InvalidOperationException();

            return result;
        }
    }
}
