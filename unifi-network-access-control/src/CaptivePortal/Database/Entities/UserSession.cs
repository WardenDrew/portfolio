using System.ComponentModel.DataAnnotations.Schema;

namespace CaptivePortal.Database.Entities
{
    public class UserSession
    {
        public int Id { get; set; }

        [ForeignKey(nameof(User))]
        public int UserId { get; set; }
        private User? _user;
        public User User
        {
            set => _user = value;
            get => _user ?? throw new InvalidOperationException();
        }

        public Guid RefreshToken { get; set; }
        public DateTime RefreshTokenIssuedAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
