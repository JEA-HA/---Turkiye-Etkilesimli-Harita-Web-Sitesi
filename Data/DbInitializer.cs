using Microsoft.AspNetCore.Identity;
using TurkeyCityGuide.Models;

namespace TurkeyCityGuide.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(ApplicationDbContext context, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Veritabanı oluşturulduğundan emin ol
            await context.Database.EnsureCreatedAsync();

            // Admin rolü oluştur
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Admin kullanıcısı oluştur
            if (await userManager.FindByNameAsync("admin") == null)
            {
                var adminUser = new AppUser
                {
                    UserName = "admin",
                    Email = "admin@turkeycityguide.com",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Şehirleri kontrol et ve ekle
            var cities = new List<City>
            {
                new City
                {
                    Name = "Ankara",
                    PlateCode = 6,
                    Region = "İç Anadolu",
                    Population = 5663322,
                    AreaKm2 = 25632,
                    DistrictCount = 25,
                    Elevation = 938,
                    Description = "Türkiye'nin başkenti ve ikinci en kalabalık şehri. Tarihi ve kültürel zenginlikleriyle ünlüdür."
                },
                new City
                {
                    Name = "İstanbul",
                    PlateCode = 34,
                    Region = "Marmara",
                    Population = 15519267,
                    AreaKm2 = 5461,
                    DistrictCount = 39,
                    Elevation = 100,
                    Description = "Türkiye'nin en kalabalık şehri. Asya ve Avrupa kıtalarını birleştiren köprüleriyle ünlüdür."
                },
                new City
                {
                    Name = "İzmir",
                    PlateCode = 35,
                    Region = "Ege",
                    Population = 4425789,
                    AreaKm2 = 11811,
                    DistrictCount = 30,
                    Elevation = 25,
                    Description = "Ege Bölgesi'nin en büyük şehri. Deniz kenarı ve tarihi yerleriyle ünlüdür."
                },
                new City
                {
                    Name = "Adıyaman",
                    PlateCode = 2,
                    Region = "Güneydoğu Anadolu",
                    Population = 611037,
                    AreaKm2 = 7644,
                    DistrictCount = 9,
                    Elevation = 669,
                    Description = "Nemrut Dağı ile ünlü, tarihi ve kültürel zenginliklere sahip Güneydoğu Anadolu şehrimiz."
                }
            };

            foreach (var city in cities)
            {
                if (!context.Cities.Any(c => c.Name == city.Name))
                {
                    context.Cities.Add(city);
                }
            }
            await context.SaveChangesAsync();

            // İlçeleri ekle
            var ankara = context.Cities.First(c => c.Name == "Ankara");
            var istanbul = context.Cities.First(c => c.Name == "İstanbul");
            var izmir = context.Cities.First(c => c.Name == "İzmir");
            var adiyaman = context.Cities.First(c => c.Name == "Adıyaman");

            var districts = new List<District>();

            if (!context.Districts.Any(d => d.CityId == ankara.Id))
            {
                districts.AddRange(new[]
                {
                    new District { Name = "Çankaya", CityId = ankara.Id },
                    new District { Name = "Keçiören", CityId = ankara.Id },
                    new District { Name = "Yenimahalle", CityId = ankara.Id },
                    new District { Name = "Mamak", CityId = ankara.Id },
                    new District { Name = "Sincan", CityId = ankara.Id }
                });
            }

            if (!context.Districts.Any(d => d.CityId == istanbul.Id))
            {
                districts.AddRange(new[]
                {
                    new District { Name = "Kadıköy", CityId = istanbul.Id },
                    new District { Name = "Beşiktaş", CityId = istanbul.Id },
                    new District { Name = "Şişli", CityId = istanbul.Id },
                    new District { Name = "Beyoğlu", CityId = istanbul.Id },
                    new District { Name = "Üsküdar", CityId = istanbul.Id }
                });
            }

            if (!context.Districts.Any(d => d.CityId == izmir.Id))
            {
                districts.AddRange(new[]
                {
                    new District { Name = "Konak", CityId = izmir.Id },
                    new District { Name = "Bornova", CityId = izmir.Id },
                    new District { Name = "Karşıyaka", CityId = izmir.Id },
                    new District { Name = "Buca", CityId = izmir.Id },
                    new District { Name = "Alsancak", CityId = izmir.Id }
                });
            }

            if (!context.Districts.Any(d => d.CityId == adiyaman.Id))
            {
                districts.AddRange(new[]
                {
                    new District { Name = "Merkez", CityId = adiyaman.Id },
                    new District { Name = "Besni", CityId = adiyaman.Id },
                    new District { Name = "Çelikhan", CityId = adiyaman.Id },
                    new District { Name = "Gerger", CityId = adiyaman.Id },
                    new District { Name = "Gölbaşı", CityId = adiyaman.Id },
                    new District { Name = "Kahta", CityId = adiyaman.Id },
                    new District { Name = "Samsat", CityId = adiyaman.Id },
                    new District { Name = "Sincik", CityId = adiyaman.Id },
                    new District { Name = "Tut", CityId = adiyaman.Id }
                });
            }

            if (districts.Any())
            {
                context.Districts.AddRange(districts);
                await context.SaveChangesAsync();
            }
        }
    }
}
