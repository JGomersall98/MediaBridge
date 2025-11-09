namespace MediaBridge.Models.Authentication
{
    public class LoginResponse
    {
        public bool IsSuccess { get; set; }
        public string? Reason { get; set; }
        public string? Token { get; set; }
        public bool IsDefaultPassword { get; set; }
    }
}
