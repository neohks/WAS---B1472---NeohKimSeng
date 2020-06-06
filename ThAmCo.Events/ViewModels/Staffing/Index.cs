using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThAmCo.Events.ViewModels.Staffing
{
    public class Index
    {
        public Index(List<Data.Staffing> staffs, int staffid, int eventid, string staffName, string eventTitle)
        {
            Staffs = staffs;
            StaffId = staffid;
            EventId = eventid;
            StaffName = staffName;
            EventTitle = eventTitle;
        }

        public List<Data.Staffing> Staffs { get; set; }

        public int StaffId { get; set; }
        public string StaffName { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; }
    }
}
