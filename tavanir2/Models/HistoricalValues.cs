using System;

namespace tavanir2.Models
{
    public class HistoricalValues
    {
        public Guid Id { get; set; }
        public string TimeSeriesId { get; set; }
        public int RowIndex { get; set; }
        public string RecivedValue { get; set; }
        public string Approved { get; set; }
        public DateTime Receiption { get; set; }
        public Guid DataMemberId { get; set; }
    }
}
