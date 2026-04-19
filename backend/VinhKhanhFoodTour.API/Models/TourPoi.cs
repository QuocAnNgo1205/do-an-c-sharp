using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VinhKhanhFoodTour.Models
{
    public class TourPoi
    {
        [Required]
        public int TourId { get; set; }

        [Required]
        public int PoiId { get; set; }

        /// <summary>1-based index defining the sequence of the POI in the tour route.</summary>
        [Required]
        public int OrderIndex { get; set; }

        // Navigation properties
        [ForeignKey("TourId")]
        public virtual Tour? Tour { get; set; }

        [ForeignKey("PoiId")]
        public virtual Poi? Poi { get; set; }
    }
}
