using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ThAmCo.Events.ViewModels.Venues
{
    public class VenueCreateModel
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }
        public string TypeId { get; set; }

        public string Date { get; set; }

        [Required]
        public string VenueCode { get; set; }

        [Required]
        public TimeSpan? Duration { get; set; }


    }
}
