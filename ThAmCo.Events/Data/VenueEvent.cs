using System;
using System.ComponentModel.DataAnnotations;

namespace ThAmCo.Events.Data
{
    public class VenueEvent
    {
        [Key, MinLength(5), MaxLength(5)]
        public string Code { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Range(1, Int32.MaxValue)]
        public int Capacity { get; set; }
    }
}
