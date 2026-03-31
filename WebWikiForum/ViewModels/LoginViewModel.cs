using System.ComponentModel.DataAnnotations;

namespace WebWikiForum.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string ReturnUrl { get; set; }
    }
}
