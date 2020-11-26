using System;

namespace tavanir2.Models
{
    public class TimeSeries
    {
        public string Id { get; set; }
        public int Year { get; set; }
        public byte? Month { get; set; }
        public byte? DayOfMonth { get; set; }
        public string TimeOfDay { get; set; }
        public string Enabled { get; set; }
    }
}
