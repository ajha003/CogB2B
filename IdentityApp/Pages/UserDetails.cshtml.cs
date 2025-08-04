using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IdentityApp.Data;

namespace IdentityApp.Pages
{
    [Authorize]
    public class UserDetailsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserDetailsModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public string UserName { get; set; }
        public string Email { get; set; }
        public string AboutMe { get; set; }
        public string ServiceCategory { get; set; }
        public string Location { get; set; }
        public string BannerImage { get; set; }
        public string ProfilePic { get; set; }
        public string PhoneNumber { get; set; }
        public string AlternativePhone { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                UserName = user.UserName;
                Email = user.Email;
                AboutMe = user.AboutMe;
                ServiceCategory = user.ServiceCategory;
                Location = user.Location;
                BannerImage = user.BannerImage;
                ProfilePic = user.ProfilePic;
                PhoneNumber = user.PhoneNumber;
                AlternativePhone = user.AlternativePhone;
            }
        }
    }
}