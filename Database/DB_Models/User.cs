namespace MediaBridge.Database.DB_Models
{
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; }
        public required string Salt { get; set; }
        public required string Email { get; set; }
        public bool EmailVerified { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastLogin { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }
        public DateTime? LastPasswordChange { get; set; }
    }
}
