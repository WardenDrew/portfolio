using System.ComponentModel.DataAnnotations.Schema;

namespace CaptivePortal.Database.Entities
{
    public class Device
    {
        public int Id { get; set; }

        [ForeignKey(nameof(User))]
        public int? UserId { get; set; }
        public User? User { get; set; }

        private DeviceNetwork? _deviceNetwork;
        public DeviceNetwork DeviceNetwork
        {
            get => _deviceNetwork ?? throw new InvalidOperationException();
            set => _deviceNetwork = value;
        }

        public string? NickName { get; set; }

        public bool Authorized { get; set; }
        public DateTime? AuthorizedUntil { get; set; }

        public string? DeviceMac { get; set; }
        public string? DetectedDeviceIpAddress { get; set; }

        public string? NasIpAddress { get; set; }
        public string? NasIdentifier { get; set; }
        public string? CallingStationId { get; set; }
        public string? AccountingSessionId { get; set; }
    }
}
