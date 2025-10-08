using System.ComponentModel.DataAnnotations.Schema;

namespace CaptivePortal.Database.Entities
{
    public class DeviceNetwork
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Device))]
        public int DeviceId { get; set; }
        private Device? _device;
        public Device Device
        {
            set => _device = value;
            get => _device ?? throw new InvalidOperationException();
        }

        [ForeignKey(nameof(Network))]
        public int NetworkId { get; set; }
        private Network? _network;
        public Network Network
        {
            set => _network = value;
            get => _network ?? throw new InvalidOperationException();
        }

        /*
        public required string AssignedDeviceAddress { get; set; }
        public bool ManuallyAssignedAddress { get; set; }

        public DateTime? LeaseIssuedAt { get; set; }
        public DateTime? LeaseExpiresAt { get; set; }*/
    }
}
