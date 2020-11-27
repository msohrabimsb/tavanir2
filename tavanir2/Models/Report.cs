using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace tavanir2.Models
{
    public class ReportViewModel
    {
        [Required(ErrorMessage = "تکمیل فیلد {0} ضروری است.")]
        [DataType(DataType.Text)]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "فیلد {0} بایستی از نوع یک رشته با حداقل طول {2} و حداکثر طول {1} باشد.")]
        [Display(Name = "کد پیگیری")]
        public string Code { get; set; }

        public ReportResult Result { get; set; }
    }

    public class ReportResult
    {
        public bool CodeFounded { get; set; }
        public int ApprovedCounts { get; set; }
        public int NotApproveCounts { get; set; }

        public List<ReportNotAproved> ListNotAproved { get; set; }
    }

    public class ReportNotAproved
    {
        public int RowIndex { get; set; }
        public string Name { get; set; }
        public string RecivedValue { get; set; }
        public string Message { get; set; }
    }
}
