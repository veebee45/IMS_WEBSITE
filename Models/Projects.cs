using System.ComponentModel.DataAnnotations;

namespace IMSMIS.Models
{
	public class Projects
	{
		public int Id { get; set; }

		[Required]
		[StringLength(255)]
		public string Name { get; set; }

	}
}
