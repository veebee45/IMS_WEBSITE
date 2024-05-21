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

	}
}
