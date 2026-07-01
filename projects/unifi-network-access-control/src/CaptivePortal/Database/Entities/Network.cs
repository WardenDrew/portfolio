using System.ComponentModel.DataAnnotations.Schema;

namespace CaptivePortal.Database.Entities
{
    public class Network
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public string? Description { get; set; }
        public string NetworkAddress { get; set; } = String.Empty;
        public string GatewayAddress { get; set; } = String.Empty;
        public int Vlan { get; set; }
        public int Capacity { get; set; }

        [ForeignKey(nameof(NetworkGroup))]
        public int NetworkGroupId { get; set; }
        private NetworkGroup? _networkGroup;
        public NetworkGroup NetworkGroup
        {
            set => _networkGroup = value;
            get => _networkGroup ?? throw new InvalidOperationException();
        }

        public List<DeviceNetwork> DeviceNetworks { get; set; } = new();
    }
}
