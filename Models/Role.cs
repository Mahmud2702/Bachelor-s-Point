using System.ComponentModel.DataAnnotations;

namespace Bachelor_s_Point.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Role name is required")]
        [MaxLength(50)]
        public string? RoleName { get; set; }

        [MaxLength(250)]
        public string? RoleDescription { get; set; }

        public ICollection<User>? Users { get; set; }
    }
}
