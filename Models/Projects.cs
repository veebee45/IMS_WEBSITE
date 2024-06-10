namespace IMSMIS.Models
{
	public class Projects
	{
		public int Id { get; set; }
		public string ProjectName { get; set; }
		public string ProjectId { get; set; }
		public string ProjectDescription { get; set; }
		public string ProjectType { get; set; }
		public List<string> Assignees { get; set; } = new List<string>();
		public string ImagePath { get; set; }
	}
}
