using CaptivePortal.Database.Entities;

namespace CaptivePortal.Models
{
    public class EditUserSubmitModel
    {
        public required User User { get; set; }
        public required Dictionary<NetworkGroup, bool> NetworkGroups { get; set; }
        public string? Password { get; set; }
    }
}
