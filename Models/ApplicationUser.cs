using Microsoft.AspNetCore.Identity;

namespace CloudDesk.Models
{
    public class ApplicationUser:IdentityUser
    {
        public string? DisplayName { get; set; } = string.Empty;
        public DateTime? ChatClearedAt { get; set; }
    }
}
