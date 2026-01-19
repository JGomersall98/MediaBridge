namespace MediaBridge.Models.Admin.EditUser
{
    public class EditUserRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public List<RoleUpdate>? RoleUpdate { get; set; }
    }
    public class RoleUpdate
    {
        public string? RoleValue { get; set; }
    }
}
