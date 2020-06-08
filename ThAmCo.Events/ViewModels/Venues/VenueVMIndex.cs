using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThAmCo.Venues.Data;

namespace ThAmCo.Events.ViewModels.Venues
{
    public class VenueVMIndex
    {

        public VenueVMIndex(List<VenueAvailibilityGetApi> venues, DateTime? startDate, DateTime? endDate)
        {
            Venues = venues;
            StartDate = startDate;
            EndDate = endDate;

        }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public List<VenueAvailibilityGetApi> Venues { get; set; }

    }
}
