namespace UserCrudApp.Models
{
    public class TicketAttachment
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int? createuid { get; set; }
        public DateTime? createdt { get; set; }
        public int? lmodifyby { get; set; }
        public DateTime? lmodifydt { get; set; }
        public int? deluid { get; set; }
        public DateTime? deldt { get; set; }
    }
}
