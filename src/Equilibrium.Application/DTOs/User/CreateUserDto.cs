﻿using System.ComponentModel.DataAnnotations;

namespace Equilibrium.Application.DTOs.User
{
    public class CreateUserDto
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
        public required string Username { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public required string Password { get; set; }

        public List<string> Roles { get; set; } = [];
    }
}
