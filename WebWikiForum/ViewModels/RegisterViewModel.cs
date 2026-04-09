using System.ComponentModel.DataAnnotations;

namespace WebWikiForum.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Password must be at least 5 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public string Role { get; set; } = "User";

        public string? AdminKey { get; set; }

        [Required(ErrorMessage = "6-digit Security PIN is required.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "PIN must be exactly 6 digits.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "PIN must contain only numbers.")]
        [Display(Name = "Security PIN (6 digits)")]
        public string SecurityPin { get; set; }
    }
}
