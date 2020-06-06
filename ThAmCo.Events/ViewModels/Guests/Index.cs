using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThAmCo.Events.ViewModels.Guests
{
    public class Index
    {
        public Index(List<Data.GuestBooking> guests, List<Data.Event> events, int cusid, int eventid, string cusName, string eventTitle)
        {
            Guests = guests;
            Events = events;
            CustomerId = cusid;
            EventId = eventid;
            CustomerName = cusName;
            EventTitle = eventTitle;
        }

        public List<Data.GuestBooking> Guests { get; set; }
        public List<Data.Event> Events { get; set; }

        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; }

    }
}
