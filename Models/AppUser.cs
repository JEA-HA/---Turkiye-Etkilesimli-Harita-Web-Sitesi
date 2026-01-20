using Microsoft.AspNetCore.Identity;

namespace TurkeyCityGuide.Models
{
    public class AppUser : IdentityUser
    {
        // Navigation properties
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
