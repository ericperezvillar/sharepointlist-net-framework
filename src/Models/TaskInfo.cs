using System;

namespace ListViewSharepoint.Models
{
    public class TaskInfo
    {
        public string Code { get; set; }
        public string Title { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double Rate { get; set; }        
    }
}
