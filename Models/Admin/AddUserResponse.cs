namespace MediaBridge.Models.Admin
{
    public class AddUserResponse
    {
        public bool IsSuccess { get; set; }
        public string? Reason { get; set; }
        public string? Password { get; set; }
    }
}
