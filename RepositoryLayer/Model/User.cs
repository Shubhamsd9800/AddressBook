using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RepositoryLayer.Model
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [JsonIgnore]
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Add Role field (e.g., "User" or "Admin")
        public string Role { get; set; } = "User";
    }
}
