using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TurkeyCityGuide.Models;

namespace TurkeyCityGuide.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<City> Cities { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CityPhoto> CityPhotos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Comment - District ilişkisi (opsiyonel)
            // NoAction kullanıyoruz çünkü Comments -> Districts -> Cities ve Comments -> Cities arasında cascade path var
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.District)
                .WithMany()
                .HasForeignKey(c => c.DistrictId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
