
namespace TurkeyCityGuide.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public string Content { get; set; } = null!;

        public int Rating { get; set; } // 1 - 5 yıldız

        public string Category { get; set; } = null!; // Yemek, Turistik, Genel

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int CityId { get; set; }
        public City City { get; set; } = null!;

        public int? DistrictId { get; set; } // Opsiyonel: İlçe seçilirse bu dolu olur
        public District? District { get; set; }

        public string? PhotoPath { get; set; } // Yorum fotoğrafı

        public string AppUserId { get; set; } = null!; // Identity kullanıyoruz, string olmalı
        public AppUser AppUser { get; set; } = null!;
    }
}

