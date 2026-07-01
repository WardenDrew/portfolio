using CaptivePortal.Database.Entities;

namespace CaptivePortal.Models
{
    public class EditDeviceSubmitModel
    {
        public required Device Device { get; set; }
        public required NetworkGroup NetworkGroup { get; set; }
        public Network? Network { get; set; }
        public bool AuthorizeForever { get; set; }
    }
}
