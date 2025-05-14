using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BusinessLayer.Helper;
using BusinessLayer.Interface;
using BusinessLayer.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using RepositoryLayer.DTO;
using RepositoryLayer.Model;

namespace AddressBookApplication.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly IUserBL _userBL;
        private readonly JwtHelper _jwtHelper;
        private readonly EmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        private readonly IDistributedCache _cache;



        public AuthController(IUserBL userBL, EmailService emailService, JwtHelper jwtHelper, ILogger<AuthController> logger, IDistributedCache cache)
        {
            _userBL = userBL;
            _emailService = emailService;
            _jwtHelper = jwtHelper;
            _logger = logger;
            _cache = cache;
        }


        /// <summary>
        /// Register a new user.
        /// </summary>
        /// <param name="userDto">User registration details.</param>
        /// <returns>Success message with JWT token.</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Success = false, Message = "Invalid input data" });

            var response = await _userBL.RegisterUserAsync(userDto);
            if (!response.Success)
                return BadRequest(response);

            return Ok(response);
        }

        /// <summary>
        /// Login with email and password.
        /// </summary>
        /// <param name="loginDto">User login credentials.</param>
        /// <returns>JWT token if login is successful.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var user = await _userBL.GetByEmailAsync(loginDto.Email);
            if (user == null || !PasswordHasher.VerifyPassword(loginDto.Password, user.PasswordHash))
                return Unauthorized(new ApiResponse<string>(false, "Invalid credentials", null));

            string token = _jwtHelper.GenerateToken(user.Email, user.Id, user.Role);

            // Store Token in Redis with a 2-hour expiration
            var cacheOptions = new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(2));
            await _cache.SetStringAsync($"AuthToken_{user.Id}", token, cacheOptions);

            return Ok(new ApiResponse<string>(true, "Login successful", token));
        }

        
        // Logout - Remove Token from Redis
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new ApiResponse<string>(false, "User not logged in", null));

            await _cache.RemoveAsync($"AuthToken_{userId}");
            return Ok(new ApiResponse<string>(true, "Logged out successfully", null));
        }


        // Forgot Password - Generate Reset Token
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Email))
                    return BadRequest(new ApiResponse<string>(false, "Invalid request: Email is required.", null));

                if (_emailService == null)
                    return StatusCode(500, new ApiResponse<string>(false, "Email service not available.", null));

                if (_jwtHelper == null)
                    return StatusCode(500, new ApiResponse<string>(false, "JWT service not available.", null));

                var user = await _userBL.GetByEmailAsync(request.Email);
                if (user == null)
                    return NotFound(new ApiResponse<string>(false, "User not found.", null));

                // Generate Reset Token
                string resetToken = _jwtHelper.GeneratePasswordResetToken(request.Email);
                string resetLink = $"https://yourfrontend.com/reset-password?token={resetToken}";

                _logger.LogInformation($"🔹 Reset link generated: {resetLink}");

                // Send Email
                string emailBody = $"Click <a href='{resetLink}'>here</a> to reset your password.";
                bool emailSent = await _emailService.SendEmailAsync(request.Email, "Password Reset", emailBody);

                if (!emailSent)
                {
                    _logger.LogError("❌ Failed to send email.");
                    return StatusCode(500, new ApiResponse<string>(false, "Failed to send reset email.", null));
                }

                return Ok(new ApiResponse<string>(true, "Password reset email sent successfully.", null));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Exception in ForgotPassword: {ex.Message}");
                return StatusCode(500, new ApiResponse<string>(false, $"Internal Server Error: {ex.Message}", null));
            }
        }

        // Reset Password - Validate Token & Change Password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest(new ApiResponse<string>(false, "Passwords do not match", null));

            var principal = _jwtHelper.ValidateToken(request.Token);
            if (principal == null)
                return BadRequest(new ApiResponse<string>(false, "Invalid or expired token", null));

            //var emailClaim = principal.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var emailClaim = principal.FindFirst(ClaimTypes.Email)?.Value
         ?? principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            if (string.IsNullOrEmpty(emailClaim))
                return BadRequest(new ApiResponse<string>(false, "Invalid token", null));

            var user = await _userBL.GetByEmailAsync(emailClaim);
            if (user == null)
                return NotFound(new ApiResponse<string>(false, "User not found", null));

            // Reset password securely
            string hashedPassword = PasswordHasher.HashPassword(request.NewPassword);
            await _userBL.UpdatePasswordAsync(user.Id, hashedPassword);

            return Ok(new ApiResponse<string>(true, "Password reset successfully", null));
        }
    }
}
