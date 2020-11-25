using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace tavanir2.Models
{
    public class UploadViewModel
    {
        [Required]
        [Display(Name = "فایل اکسل")]
        public IFormFile File { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [StringLength(50, MinimumLength = 1)]
        [Display(Name = "نام Sheet")]
        public string SheetName { get; set; }

        [Required]
        [Display(Name = "عنوان ستون‌ها")]
        public string ColumnsType { get; set; }
    }
}
