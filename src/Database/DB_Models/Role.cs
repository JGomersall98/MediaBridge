namespace MediaBridge.Database.DB_Models
{
    public class Role
    {
        public int Id { get; set; }
        public required string RoleValue { get; set; }

        public ICollection<UserRole> UserRole { get; set; } = new List<UserRole>();
    }
}
