namespace UserCrudApp.Models
{
    public class ApiLog
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public string Method { get; set; }
        public string User { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
