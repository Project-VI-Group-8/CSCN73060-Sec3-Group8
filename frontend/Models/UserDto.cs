namespace frontend.Models
{
    /// <summary>Lightweight DTO for deserializing user responses from the API.</summary>
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
    }
}
