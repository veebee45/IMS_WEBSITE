namespace IMSMIS.Models
{
	public class ProjectViewModel
	{
		public List<ProjectInfo> Projects { get; set; }
		public int CurrentPage { get; set; }
		public int TotalPages { get; set; }
	}
}
