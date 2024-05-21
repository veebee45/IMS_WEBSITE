using System.ComponentModel.DataAnnotations;

namespace IMSMIS.Models
{
    public partial class Tasks
    {

        public string Summary { get; set; }
        public string Description { get; set; }
        public string Project { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string Assignee { get; set; }
		public int EstimatedDays { get; set; }
        public string ProjectID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CompletionDate { get; set; }
        public string Flag { get; set; }
        public string Type { get; set; }
        public string filenames { get; set; }
        public string CreatedBy { get; set; }   
	}
}
