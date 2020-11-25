using System.ComponentModel.DataAnnotations;

namespace tavanir2.Models
{
    public class LoginViewModel
    {
        [Required]
        [DataType(DataType.Text)]
        [StringLength(50, MinimumLength = 1)]
        [Display(Name = "نام کاربری")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(50, MinimumLength = 1)]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; }
    }
}
