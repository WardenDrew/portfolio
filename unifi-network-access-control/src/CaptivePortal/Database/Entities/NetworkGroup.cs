using System.ComponentModel.DataAnnotations.Schema;

namespace CaptivePortal.Database.Entities
{
    public class NetworkGroup
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public string? Description { get; set; }
        public bool Registration { get; set; }
        public bool Guest { get; set; }
        public bool IsPool { get; set; }

        public List<Network> Networks { get; set; } = new();
        public List<UserNetworkGroup> UserNetworkGroups { get; set; } = new();
    }
}
