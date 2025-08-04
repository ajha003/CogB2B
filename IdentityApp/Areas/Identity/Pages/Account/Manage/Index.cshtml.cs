// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using IdentityApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityApp.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            [Display(Name = "About Me")]
            [StringLength(1000, ErrorMessage = "About Me cannot exceed 1000 characters.")]
            public string? AboutMe { get; set; }

            [Display(Name = "Service Category")]
            [StringLength(100, ErrorMessage = "Service Category cannot exceed 100 characters.")]
            public string? ServiceCategory { get; set; }

            [Display(Name = "Location")]
            [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters.")]
            public string? Location { get; set; }

            [Display(Name = "Banner Image URL")]
            [StringLength(500, ErrorMessage = "Banner Image URL cannot exceed 500 characters.")]
            public string? BannerImage { get; set; }

            [Display(Name = "Profile Picture URL")]
            [StringLength(500, ErrorMessage = "Profile Picture URL cannot exceed 500 characters.")]
            public string? ProfilePic { get; set; }

            [Display(Name = "Alternative Phone")]
            [Phone]
            [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
            public string? AlternativePhone { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                AboutMe = user.AboutMe,
                ServiceCategory = user.ServiceCategory,
                Location = user.Location,
                BannerImage = user.BannerImage,
                ProfilePic = user.ProfilePic,
                AlternativePhone = user.AlternativePhone
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            // Update custom properties
            if (Input.AboutMe != user.AboutMe)
                user.AboutMe = Input.AboutMe;
            if (Input.ServiceCategory != user.ServiceCategory)
                user.ServiceCategory = Input.ServiceCategory;
            if (Input.Location != user.Location)
                user.Location = Input.Location;
            if (Input.BannerImage != user.BannerImage)
                user.BannerImage = Input.BannerImage;
            if (Input.ProfilePic != user.ProfilePic)
                user.ProfilePic = Input.ProfilePic;
            if (Input.AlternativePhone != user.AlternativePhone)
                user.AlternativePhone = Input.AlternativePhone;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                StatusMessage = "Unexpected error when trying to update profile.";
                return RedirectToPage();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
