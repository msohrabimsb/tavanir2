using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace tavanir2.Models
{
    public class Report
    {
        [Required]
        [DataType(DataType.Text)]
        [StringLength(10, MinimumLength = 10)]
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
