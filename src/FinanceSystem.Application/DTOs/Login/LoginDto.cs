﻿using System.ComponentModel.DataAnnotations;

namespace FinanceSystem.Application.DTOs.Login
{
    public class LoginDto
    {
        [Required]
        public required string Username { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}
