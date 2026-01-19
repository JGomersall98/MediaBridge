namespace MediaBridge.Models.Admin.ResetPassword
{
    public class ResetPasswordRequest
    {
        public required int UserId { get; set; }
        public required string CurrentPassword { get; set; }
        public required string NewPassword { get; set; }
        public required string ConfirmPassword { get; set; }
    }
}
