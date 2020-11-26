using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
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

        [Display(Name = "عنوان ستون‌ها")]
        public string ColumnsType { get; set; }

        [Required]
        [Display(Name = "گروه داده‌ای")]
        public Guid DataCategoryID { get; set; }

        public List<DataCategories> ListDataCategories { get; set; }
    }

    public class DataCategories
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
