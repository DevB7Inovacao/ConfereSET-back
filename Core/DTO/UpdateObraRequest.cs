namespace Core.DTO
{
	public class UpdateObraRequest
	{
		public string? Name { get; set; }
		public string? StreetAddress { get; set; }
		public string? Number { get; set; }
		public string? AddressLine2 { get; set; }
		public string? Neighborhood { get; set; }
		public string? City { get; set; }
		public string? State { get; set; }
		public string? PostalCode { get; set; }
		public string? Country { get; set; }
		public string? ClientName { get; set; }
		public string? ClientEmail { get; set; }
		public string? ClientPhone { get; set; }
		public string? ClientDocument { get; set; }
		public DateTime? StartDate { get; set; }
		public string? NameCompany { get; set; }
	}
}
