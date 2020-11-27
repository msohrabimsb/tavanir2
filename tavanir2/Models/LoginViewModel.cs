using System.ComponentModel.DataAnnotations;

namespace tavanir2.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "تکمیل فیلد {0} ضروری است.")]
        [DataType(DataType.Text)]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "فیلد {0} بایستی از نوع یک رشته با حداقل طول {2} و حداکثر طول {1} باشد.")]
        [Display(Name = "نام کاربری")]
        public string Username { get; set; }

        [Required(ErrorMessage = "تکمیل فیلد {0} ضروری است.")]
        [DataType(DataType.Password)]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "فیلد {0} بایستی از نوع یک رشته با حداقل طول {2} و حداکثر طول {1} باشد.")]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; }
    }
}
