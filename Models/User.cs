namespace IMSMIS.Models
{
	public class User
	{
		 public int UserId { get; set; }
        public string? EmailId { get; set; }
        public string? Password { get; set; }
        public DateTime? Date { get; set; }
        public string? UserRole {  get; set; }
        public string? FirstName { get; set;}
        public string? LastName { get; set;}


	}
}
