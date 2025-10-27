using IdentityManager.DTOs;
using IdentityManager.Models;
using IdentityManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IdentityManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            JwtService jwtService,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "User with this email already exists."
                    });
                }

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    EmailConfirmed = false // Require email confirmation
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    return BadRequest(new AuthResponse
                    {
                        IsSuccess = false,
                        Message = string.Join(", ", result.Errors.Select(e => e.Description))
                    });
                }

                // Send email confirmation
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = $"{_configuration["AppUrl"]}/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
                await _emailService.SendEmailConfirmationAsync(user.Email!, confirmationLink);

                return Ok(new AuthResponse
                {
                    IsSuccess = true,
                    Message = "Registration successful. Please check your email to confirm your account."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse
                {
                    IsSuccess = false,
                    Message = "An error occurred during registration."
                });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return BadRequest(new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Invalid email or password."
                    });
                }

                if (!user.IsActive)
                {
                    return BadRequest(new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Account is deactivated."
                    });
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
                if (!result.Succeeded)
                {
                    return BadRequest(new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Invalid email or password."
                    });
                }

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Generate tokens
                var accessToken = await _jwtService.GenerateAccessTokenAsync(user);
                var refreshToken = _jwtService.GenerateRefreshToken();

                // Save refresh token
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _userManager.UpdateAsync(user);

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new AuthResponse
                {
                    IsSuccess = true,
                    Message = "Login successful.",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:AccessTokenExpirationMinutes"])),
                    User = new UserInfo
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email!,
                        ProfilePicture = user.ProfilePicture,
                        Roles = roles.ToList(),
                        CreatedAt = user.CreatedAt,
                        LastLoginAt = user.LastLoginAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse
                {
                    IsSuccess = false,
                    Message = "An error occurred during login."
                });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponse>> RefreshToken(RefreshTokenRequest request)
        {
            try
            {
                var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
                if (principal == null)
                {
                    return BadRequest(new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Invalid access token."
                    });
                }

                var userId = principal.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Invalid token claims."
                    });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    return BadRequest(new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Invalid refresh token."
                    });
                }

                // Generate new tokens
                var newAccessToken = await _jwtService.GenerateAccessTokenAsync(user);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                // Update refresh token
                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _userManager.UpdateAsync(user);

                return Ok(new AuthResponse
                {
                    IsSuccess = true,
                    Message = "Token refreshed successfully.",
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:AccessTokenExpirationMinutes"]))
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse
                {
                    IsSuccess = false,
                    Message = "An error occurred while refreshing token."
                });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult<AuthResponse>> ForgotPassword(ForgotPasswordRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    // Don't reveal if user exists or not
                    return Ok(new AuthResponse
                    {
                        IsSuccess = true,
                        Message = "If your email is registered, you will receive a password reset link."
                    });
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = $"{_configuration["AppUrl"]}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";
                
                var emailSent = await _emailService.SendPasswordResetEmailAsync(user.Email!, resetLink);
                
                if (emailSent)
                {
                    return Ok(new AuthResponse
                    {
                        IsSuccess = true,
                        Message = "Password reset link has been sent to your email."
                    });
                }
                else
                {
                    return StatusCode(500, new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Failed to send password reset email. Please try again."
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse
                {
                    IsSuccess = false,
                    Message = "An error occurred while processing your request."
                });
            }
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<AuthResponse>> ResetPassword(ResetPasswordRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return BadRequest(new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Invalid email address."
                    });
                }

                var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
                if (!result.Succeeded)
                {
                    return BadRequest(new AuthResponse
                    {
                        IsSuccess = false,
                        Message = string.Join(", ", result.Errors.Select(e => e.Description))
                    });
                }

                return Ok(new AuthResponse
                {
                    IsSuccess = true,
                    Message = "Password has been reset successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse
                {
                    IsSuccess = false,
                    Message = "An error occurred while resetting password."
                });
            }
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<ActionResult<AuthResponse>> ChangePassword(ChangePasswordRequest request)
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Invalid user token."
                    });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return BadRequest(new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "User not found."
                    });
                }

                var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
                if (!result.Succeeded)
                {
                    return BadRequest(new AuthResponse
                    {
                        IsSuccess = false,
                        Message = string.Join(", ", result.Errors.Select(e => e.Description))
                    });
                }

                return Ok(new AuthResponse
                {
                    IsSuccess = true,
                    Message = "Password changed successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse
                {
                    IsSuccess = false,
                    Message = "An error occurred while changing password."
                });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult<AuthResponse>> Logout()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        // Clear refresh token
                        user.RefreshToken = null;
                        user.RefreshTokenExpiryTime = null;
                        await _userManager.UpdateAsync(user);
                    }
                }

                return Ok(new AuthResponse
                {
                    IsSuccess = true,
                    Message = "Logged out successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse
                {
                    IsSuccess = false,
                    Message = "An error occurred during logout."
                });
            }
        }

        [HttpGet("confirm-email")]
        public async Task<ActionResult<AuthResponse>> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return BadRequest(new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Invalid user ID."
                    });
                }

                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (!result.Succeeded)
                {
                    return BadRequest(new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Email confirmation failed."
                    });
                }

                return Ok(new AuthResponse
                {
                    IsSuccess = true,
                    Message = "Email confirmed successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse
                {
                    IsSuccess = false,
                    Message = "An error occurred during email confirmation."
                });
            }
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<UserInfo>> GetProfile()
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest("Invalid user token.");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new UserInfo
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email!,
                    ProfilePicture = user.ProfilePicture,
                    Roles = roles.ToList(),
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving profile.");
            }
        }
    }
}