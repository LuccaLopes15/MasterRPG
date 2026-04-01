using Microsoft.AspNetCore.Identity;

namespace T20Master.Web.Data;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
