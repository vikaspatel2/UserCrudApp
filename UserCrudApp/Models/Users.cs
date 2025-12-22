namespace UserCrudApp.Models
{
    public class Users
    {
        
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public int? createuid { get; set; }
        public DateTime? createdt { get; set; }
        public int? lmodifyby { get; set; }
        public DateTime? lmodifydt { get; set; }
        public int? deluid { get; set; }
        public DateTime? deldt { get; set; }
    }

}
