namespace TurkeyCityGuide.Models
{
    public class City
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public int PlateCode { get; set; }
        public string? Region { get; set; }
        public int Population { get; set; }
        public double AreaKm2 { get; set; }
        public int DistrictCount { get; set; }
        public int Elevation { get; set; }
        public string? DistrictMapImage { get; set; }

        public string? Description { get; set; }

        // Navigation properties
        public ICollection<District> Districts { get; set; } = new List<District>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<CityPhoto> Photos { get; set; } = new List<CityPhoto>();
    }
}
