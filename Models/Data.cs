using System.ComponentModel.DataAnnotations;

namespace IMSMIS.Models
{
	public partial class Data
	{
		public string Project {  get; set; }
		public string ProjectID { get; set; }
		public int PrdcessedData { get; set; }
		public int RejectedData { get; set; }
		public int totaldata { get; set; }
		public string statename { get; set; }
		public int year { get; set; }
		public string month { get; set; }
	}

	public partial class Data1
	{
		public string TeamName { get; set; }
		public int PendingTask { get; set; }

	}
}
