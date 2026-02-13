namespace UserCrudApp.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }  
        public bool IsBreached { get; set; }  
        public string Priority { get; set; }  
        public int UserId { get; set; } 
        public int? AssignedTo { get; set; }
        public string AssignedEmail { get; set; }
        public int? createuid { get; set; }
        public DateTime? createdt { get; set; }
        public int? lmodifyby { get; set; }
        public DateTime? lmodifydt { get; set; }
        public int? deluid { get; set; }
        public DateTime? deldt { get; set; }
        public DateTime? DueDate { get; set; }
        public virtual List<TicketReply> Replies { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
    }

    public class TicketEmailInfo
    {
        public int UserId { get; set; }
        public string OwnerEmail { get; set; }
        public string AssignedEmail { get; set; }
    }

}
