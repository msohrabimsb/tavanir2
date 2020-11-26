using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace tavanir2.Models
{
    public class RegisterAccountViewModel
    {
        [Required]
        [DataType(DataType.Text)]
        [StringLength(10, MinimumLength = 1)]
        [Display(Name = "کد شرکت")]
        public string Code { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [StringLength(50, MinimumLength = 1)]
        [Display(Name = "نام شرکت")]
        public string Name { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [StringLength(50, MinimumLength = 1)]
        [Display(Name = "نام کاربری")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [StringLength(50, MinimumLength = 1)]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; }

        [Required]
        [Display(Name = "موقعیت مکانی")]
        public string LocationId { get; set; }

        [Display(Name = "توضیحات")]
        public string Description { get; set; }

        public List<Locations> Locations { get; set; }
    }
}
