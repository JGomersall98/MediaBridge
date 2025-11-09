using MediaBridge.Database.DB_Models;

namespace MediaBridge.Models.Admin.GetUser
{
    public class GetUserResponse : StandardResponse
    {
        public List<UserResponse>? UserResponse { get; set;}
    }
    public class UserResponse
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
    }
}
