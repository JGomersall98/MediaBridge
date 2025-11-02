namespace MediaBridge.Models.Admin
{
    public class PasswordResponse
    {
        public string Password { get; set; } = default!;
        public string Salt { get; set; } = default!;
        public string Hash { get; set; } = default!;
    }
}
