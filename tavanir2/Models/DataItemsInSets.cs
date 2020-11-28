using System;

namespace tavanir2.Models
{
    public class DataMembers
    {
        public Guid Id { get; set; }
        public string ColumnValue { get; set; }
        public string ColumnName { get; set; }
        public string Title { get; set; }
        public Guid DataSetId { get; set; }
        public Guid DataItemId { get; set; }
        public string RegularExperssion { get; set; }
        public string Description { get; set; }
    }
}
