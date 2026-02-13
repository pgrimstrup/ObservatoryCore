using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace StarGazer.Colonization
{
    public class ColonizationGridItem
    {
        public required string Commodity { get; set; }

        [Display(Name = "Initial Required")]
        public string? InitiallyRequired { get; set; }

        [Display(Name = "Currently Required")]
        public string? CurrentlyRequired { get; set; }

        [Display(Name = "In Carrier")]
        public string? QtyInCarrier { get; set; }

        [Display(Name = "In Cargo")]
        public string? QtyInCargo { get; set; }

        [Display(Name = "Needed")]
        public string? QtyNeeded { get; set; }
    }
}
