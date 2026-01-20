namespace TurkeyCityGuide.Models
{
    public class CityPhoto
    {
        public int Id { get; set; }

        public string ImagePath { get; set; } = null!; // wwwroot/images/cities/ankara/photo1.jpg

        public string? Caption { get; set; } // Fotoğraf açıklaması

        public int CityId { get; set; }
        public City City { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
