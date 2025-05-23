﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryLayer.DTO
{
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "New Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
