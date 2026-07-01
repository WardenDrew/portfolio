using CaptivePortal.Models;

namespace CaptivePortal.Database.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = String.Empty;
        public string Email { get; set; } = String.Empty;
        public string Hash { get; set; } = String.Empty;
        public bool ChangePasswordNextLogin { get; set; }
        public PermissionLevel PermissionLevel { get; set; }

        public List<Device> Devices { get; set; } = new();
        public List<UserSession> UserSessions { get; set; } = new();
        public List<UserNetworkGroup> UserNetworkGroups { get; set; } = new();
    }
}
