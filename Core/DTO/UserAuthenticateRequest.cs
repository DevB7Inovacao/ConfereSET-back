namespace Core.DTO
{
	public class UserAuthenticateRequest
	{
		public required string Password { get; set; }
		public required string Email { get; set; }
	}
}
