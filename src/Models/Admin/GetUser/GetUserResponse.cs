using Microsoft.AspNetCore.Components.Web;

namespace MediaBridge.Models.Admin.GetUser
{
    public class GetUserResponse : StandardResponse
    {
        public UserInfo? UserInfo {  get; set; }
    }
    public class UserInfo
    {
        public int? Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Created {  get; set; }
        public string? LastLogin { get; set; }
        public string? LastPasswordChange { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
        public List<GetUserRoles>? RoleList { get; set; }
    }
    public class GetUserRoles
    {
        public string? Name { get; set; }
    }
}
