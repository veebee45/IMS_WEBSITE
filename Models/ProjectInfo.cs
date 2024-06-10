namespace IMSMIS.Models
{
    public class ProjectInfo
    {
        public string ProjectID { get; set; }
        public string Flag { get; set; }
        public string Content1 { get; set; }
        public string Content2 { get; set; }
        public string Assigner { get; set; }
        public string Assignee { get; set; }

        public string Message { get; set;}

        public int NID { get; set; }
        public DateTime create_at {  get; set; }
    }
}
