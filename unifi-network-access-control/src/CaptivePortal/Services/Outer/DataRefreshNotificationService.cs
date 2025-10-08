namespace CaptivePortal.Services.Outer
{
    public class DataRefreshNotificationService
    {
        public event EventHandler? NetworkUsageNotification;
        public void NetworkUsageNotify() 
            => NetworkUsageNotification?.Invoke(this, EventArgs.Empty);

        public event EventHandler? DeviceDetailsNotification;
        public void DeviceDetailsNotify()
            => DeviceDetailsNotification?.Invoke(this, EventArgs.Empty);

        public event EventHandler? UserDetailsNotification;
        public void UserDetailsNotify()
            => UserDetailsNotification?.Invoke(this, EventArgs.Empty);
    }
}
