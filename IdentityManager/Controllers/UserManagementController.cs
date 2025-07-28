using IdentityManager.DTOs;
using IdentityManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdentityManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UserManagementController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UserManagementController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet("users")]
        public async Task<ActionResult<PagedUserResponse>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            try
            {
                var query = _userManager.Users.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u => 
                        u.FirstName.Contains(search) || 
                        u.LastName.Contains(search) || 
                        u.Email!.Contains(search));
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new UserInfo
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email!,
                        ProfilePicture = u.ProfilePicture,
                        CreatedAt = u.CreatedAt,
                        LastLoginAt = u.LastLoginAt
                    })
                    .ToListAsync();

                // Get roles for each user
                foreach (var user in users)
                {
                    var appUser = await _userManager.FindByIdAsync(user.Id);
                    if (appUser != null)
                    {
                        user.Roles = (await _userManager.GetRolesAsync(appUser)).ToList();
                    }
                }

                return Ok(new PagedUserResponse
                {
                    Users = users,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving users.");
            }
        }

        [HttpGet("users/{id}")]
        public async Task<ActionResult<UserInfo>> GetUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
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
                return StatusCode(500, "An error occurred while retrieving user.");
            }
        }

        [HttpPut("users/{id}/status")]
        public async Task<ActionResult<AuthResponse>> UpdateUserStatus(string id, [FromBody] UpdateUserStatusRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                user.IsActive = request.IsActive;
                await _userManager.UpdateAsync(user);

                return Ok(new AuthResponse
                {
                    IsSuccess = true,
                    Message = $"User status updated successfully. User is now {(request.IsActive ? "active" : "inactive")}."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse
                {
                    IsSuccess = false,
                    Message = "An error occurred while updating user status."
                });
            }
        }

        [HttpPost("users/{id}/roles")]
        public async Task<ActionResult<AuthResponse>> AssignRoles(string id, [FromBody] AssignRolesRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                var currentRoles = await _userManager.GetRolesAsync(user);
                var rolesToRemove = currentRoles.Except(request.Roles);
                var rolesToAdd = request.Roles.Except(currentRoles);

                if (rolesToRemove.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                }

                if (rolesToAdd.Any())
                {
                    await _userManager.AddToRolesAsync(user, rolesToAdd);
                }

                return Ok(new AuthResponse
                {
                    IsSuccess = true,
                    Message = "User roles updated successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse
                {
                    IsSuccess = false,
                    Message = "An error occurred while assigning roles."
                });
            }
        }

        [HttpGet("roles")]
        public async Task<ActionResult<List<RoleInfo>>> GetRoles()
        {
            try
            {
                var roles = await _roleManager.Roles
                    .Where(r => r.IsActive)
                    .Select(r => new RoleInfo
                    {
                        Id = r.Id,
                        Name = r.Name!,
                        Description = r.Description,
                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();

                return Ok(roles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving roles.");
            }
        }

        [HttpPost("roles")]
        public async Task<ActionResult<AuthResponse>> CreateRole([FromBody] CreateRoleRequest request)
        {
            try
            {
                var existingRole = await _roleManager.FindByNameAsync(request.Name);
                if (existingRole != null)
                {
                    return BadRequest(new AuthResponse
                    {
                        IsSuccess = false,
                        Message = "Role with this name already exists."
                    });
                }

                var role = new ApplicationRole
                {
                    Name = request.Name,
                    Description = request.Description
                };

                var result = await _roleManager.CreateAsync(role);
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
                    Message = "Role created successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse
                {
                    IsSuccess = false,
                    Message = "An error occurred while creating role."
                });
            }
        }

        [HttpDelete("users/{id}")]
        public async Task<ActionResult<AuthResponse>> DeleteUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                var result = await _userManager.DeleteAsync(user);
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
                    Message = "User deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AuthResponse
                {
                    IsSuccess = false,
                    Message = "An error occurred while deleting user."
                });
            }
        }
    }

    // Additional DTOs for user management
    public class PagedUserResponse
    {
        public List<UserInfo> Users { get; set; } = new List<UserInfo>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class UpdateUserStatusRequest
    {
        public bool IsActive { get; set; }
    }

    public class AssignRolesRequest
    {
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class RoleInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}