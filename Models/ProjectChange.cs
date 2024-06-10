namespace IMSMIS.Models
{

	public class ProjectChange
	{
		public int ChangeID {  get; set; }
		public string UserName { get; set; }
		public string Field { get; set; }
		public string OldValue { get; set; }
		public string NewValue { get; set; }
		public DateTime Time { get; set; }
	}
}
