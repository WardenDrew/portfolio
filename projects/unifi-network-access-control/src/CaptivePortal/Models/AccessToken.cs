namespace CaptivePortal.Models
{
    public class AccessToken
    {
        public int UserId { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public PermissionLevel PermissionLevel { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public Guid RefreshToken { get; set; }
        public DateTime RefreshTokenIssuedAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
