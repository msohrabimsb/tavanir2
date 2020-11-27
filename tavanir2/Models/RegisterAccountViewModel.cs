using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace tavanir2.Models
{
    public class RegisterAccountViewModel
    {
        [Required(ErrorMessage = "تکمیل فیلد {0} ضروری است.")]
        [DataType(DataType.Text)]
        [RegularExpression("^[0-9]{1,6}$", ErrorMessage ="مقدار {0} وارد شده صحیح نمی‌باشد. مقدار قابل قبول عددی به طول 1 تا 6 رقم می‌باشد.")]
        [StringLength(6, MinimumLength = 1, ErrorMessage = "فیلد {0} بایستی از نوع یک رشته با حداقل طول {2} و حداکثر طول {1} باشد.")]
        [Display(Name = "کد شرکت")]
        public string Code { get; set; }

        [Required(ErrorMessage = "تکمیل فیلد {0} ضروری است.")]
        [DataType(DataType.Text)]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "فیلد {0} بایستی از نوع یک رشته با حداقل طول {2} و حداکثر طول {1} باشد.")]
        [Display(Name = "نام شرکت")]
        public string Name { get; set; }

        [Required(ErrorMessage = "تکمیل فیلد {0} ضروری است.")]
        [DataType(DataType.Text)]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "فیلد {0} بایستی از نوع یک رشته با حداقل طول {2} و حداکثر طول {1} باشد.")]
        [Display(Name = "نام کاربری")]
        public string Username { get; set; }

        [Required(ErrorMessage = "تکمیل فیلد {0} ضروری است.")]
        [DataType(DataType.Text)]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "فیلد {0} بایستی از نوع یک رشته با حداقل طول {2} و حداکثر طول {1} باشد.")]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; }

        [Required(ErrorMessage = "تعیین {0} ضروری است.")]
        [Display(Name = "موقعیت مکانی")]
        public string LocationId { get; set; }

        [Display(Name = "توضیحات")]
        public string Description { get; set; }

        public List<Locations> Locations { get; set; }
    }
}
