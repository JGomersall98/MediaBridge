using System.ComponentModel.DataAnnotations;

namespace MediaBridge.Models.Admin.AddUser
{
    public class AddUserRequest
    {
        [Required]
        public required string UserName { get; set; }
        [Required]
        public required string Email { get; set; }

    }
}
