using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace IdentityApp.Data;

public class ApplicationUser : IdentityUser
{
    [Display(Name = "About Me")]
    [StringLength(1000, ErrorMessage = "About Me cannot exceed 1000 characters.")]
    public string? AboutMe { get; set; }

    [Display(Name = "Service Category")]
    [StringLength(100, ErrorMessage = "Service Category cannot exceed 100 characters.")]
    public string? ServiceCategory { get; set; }

    [Display(Name = "Location")]
    [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters.")]
    public string? Location { get; set; }

    [Display(Name = "Banner Image")]
    [StringLength(500, ErrorMessage = "Banner Image URL cannot exceed 500 characters.")]
    public string? BannerImage { get; set; }

    [Display(Name = "Profile Picture")]
    [StringLength(500, ErrorMessage = "Profile Picture URL cannot exceed 500 characters.")]
    public string? ProfilePic { get; set; }

    // Note: PhoneNumber is already available in IdentityUser, but we can add additional phone-related properties if needed
    [Display(Name = "Alternative Phone")]
    [Phone]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    public string? AlternativePhone { get; set; }
}