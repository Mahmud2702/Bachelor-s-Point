namespace Bachelor_s_Point.Application.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }

        public string? UserName { get; set; }

        public string? Email { get; set; }

        public string? Address { get; set; }

        public int RoleId { get; set; }

        public string? RoleName { get; set; }

        public DateTime? LastLogin { get; set; }
    }
}
