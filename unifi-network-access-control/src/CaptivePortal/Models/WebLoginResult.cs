namespace CaptivePortal.Models
{
    public class WebLoginResult
    {
        public bool Success { get; set; }
        public AccessToken? AccessToken { get; set; }
        public bool ChangePasswordRequired { get; set; }
    }
}
