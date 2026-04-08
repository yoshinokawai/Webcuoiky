using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebWikiForum.Data;
using WebWikiForum.Models;
using WebWikiForum.Services;
using Microsoft.Extensions.Options;
using WebWikiForum;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options => {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResource));
    });

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IActivityService, ActivityService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });
var app = builder.Build();

// Seed database with Agencies if empty
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.Migrate(); // Ensure DB is up to date
        if (!db.Agencies.Any())
        {
            db.Agencies.AddRange(
                new Agency { Name = "Hololive Production", LogoUrl = "https://upload.wikimedia.org/wikipedia/commons/e/e6/Hololive_Production_logo.png", Region = "Japan", Focus = "Idol, Gaming", Description = "Pioneers of the idol-centric VTuber model.", TalentCount = 80 },
                new Agency { Name = "NIJISANJI", LogoUrl = "https://upload.wikimedia.org/wikipedia/commons/e/e3/ANYCOLOR_Inc_logo.png", Region = "Japan", Focus = "Variety, Chat", Description = "Known for its massive roster and variety of streamers.", TalentCount = 200 },
                new Agency { Name = "VShojo", LogoUrl = "https://upload.wikimedia.org/wikipedia/en/thumb/0/07/VShojo_logo.png/250px-VShojo_logo.png", Region = "US", Focus = "Streamer-led", Description = "A talent-first agency focusing on creator freedom and IP ownership.", TalentCount = 14 }
            );
            db.SaveChanges();
        }

        if (!db.Vtubers.Any(v => v.IsIndependent))
        {
            db.Vtubers.AddRange(
                new Vtuber { Name = "Shylily", Age = 24, DebutDate = new DateTime(2022, 1, 11), Birthday = "January 11", AvatarUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuBcwqJmvI6QRhBoLIvALsqquG49TuN0g7UBVPkcoSNKKy9vCM6dN1_VrD4lpd7U7We5VcrAh0xyQ_pF7ZRJWi9xRe8pGEIncoMe78B52USIo5eq2dLHcBLuwQnGtyHU8D72b_9E38dspsL3CjHGaANusFkBvfszldyi6RRlEtSikuNm-3uo1iLiB8rsBRVu4OLrXFtBNSTOLkpM5nOLYuI9Gy3NJhrtjwM8Y0D1FbZ8Ql6_6Tu4OtbFSTWFM7Wrl_dk-1yA5gTxYzE", Status = "Approved", Language = "English / German", Region = "Europe", Tags = "Gaming, Variety", IsIndependent = true, Lore = "Orca VTuber based in Europe. Known for her high-energy variety streams and unique design.", YoutubeUrl = "https://www.youtube.com/@Shylily" },
                new Vtuber { Name = "Filian", DebutDate = new DateTime(2021, 4, 24), Birthday = "April 24", AvatarUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuBLcFhjHtjvzfH9HT0uyxEO4mplc9JkEGXfi1MnszXpPgz-H-1CS4K9FZax9I9083Zckab7YXk20adSR7Q_5LfKmPviKvXCVeYL9RtOKNnz8fI3XXho5RWpEodVI02yigAjGlCjh342VeCiWWpylsw-uU5pCvPgUIplbQUCvgPpQj6fuctbDVW5_6exvXAzN-mts65aHDeZbcBazd057xYaMteCOujQcZKiWf2qaw-0Anezonht6bnpCJJIk1SjqXciVc3Am2Srpus", Status = "Approved", Language = "English", Region = "NA", Tags = "Gaming, Comedy", IsIndependent = true, Lore = "Chaotic variety streamer known for VR games and parkour. Member of the 'Mint' group.", YoutubeUrl = "https://www.youtube.com/@filian" },
                new Vtuber { Name = "Saruei", DebutDate = new DateTime(2021, 9, 17), Birthday = "September 17", AvatarUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuBnDVC6BsqkBhuyYJ2kxvNRzMuz2BoVcrDDRYoZ-XDrUcuUCjUjIwKH_IrTPr-vvyhbapDPqoxn9oNy8WNPEhGV17Ky7l8VWAtiGzmyNQ6bxxiZdjpnCnhASG4JFBD2WNOvlNMW6kXEzSXaimOBfa3fNBfZfLWGS5fqYGvXJMweu829GGAZFVMOSbgs1pHVeUmsL3N7yr1WISZiQKBToiJ_oX9PjaZu7-b2YweBF_BrHnQm8GmRK7_pK8yT1g9tNKSdhCqyhQpGib8", Status = "Approved", Language = "English / French", Region = "Europe", Tags = "Art, ASMR", IsIndependent = true, Lore = "French illustrator and VTuber. Known for her art streams and unique dark aesthetic.", YoutubeUrl = "https://www.youtube.com/@saruei_art" }
            );
            db.SaveChanges();
        }

        if (!db.News.Any())
        {
            db.News.AddRange(
                new News { Title = "Hololive Production Announces 'Hololive Super Expo 2024' Details", Type = "Event", Author = "Admin", IsFeatured = true, PublishDate = DateTime.Now.AddHours(-2), ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuC9GV3pMOtHNuxKOCKwzrAnvFc-NW4p-hJwBAQWu5MZL6S_Nf8HxLnUa2Iy__U1IEkpHFAY7p9TH3x_eI5rbMBUd4heQxUo3xa30LhafkUnzR-zBx7_C7ez0YwK4uKslcxneAXqwTQhrWJM7OmEZS3RfRjVAs7HPfIefQRF-HZ50hs65jn8gKDlLHHyxqH-TEHElThK2oI_oBH5o_CumxufJeBgII_91hMjBhJp0QCqzM-F1XObFJYwluJqUFokjdkWxCQRruMwbH8" },
                new News { Title = "New Indie VTuber Agency 'Prism Project' Teases Generation 5", Type = "Debut", Author = "Staff", PublishDate = DateTime.Now.AddHours(-5), ImageUrl = "https://lh3.googleusercontent.com/aida-public/AB6AXuAGvY6T_M_u2W9v9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8W9g7R_X_L8" },
                new News { Title = "Mori Calliope releases new EP 'JIGOKU 6' featuring diverse artists", Type = "Music", Author = "MusicTeam", PublishDate = DateTime.Now.AddDays(-1), ImageUrl = "https://i.ytimg.com/vi/6u2OyeL6h7I/maxresdefault.jpg" },
                new News { Title = "ASMR Masters: Top 10 VTubers for your Night Comfort", Type = "ASMR", Author = "Editorial", PublishDate = DateTime.Now.AddDays(-2), ImageUrl = "https://i.ytimg.com/vi/6fR0eSIn2zI/maxresdefault.jpg" },
                new News { Title = "CR Cup Overwatch 2: VTuber teams confirmed for the finals", Type = "Gaming", Author = "GamingDaily", PublishDate = DateTime.Now.AddDays(-3), ImageUrl = "https://i.ytimg.com/vi/4T_L_7L-e08/maxresdefault.jpg" }
            );
            db.SaveChanges();
        }

        if (!db.Activities.Any())
        {
            db.Activities.AddRange(
                new Activity { Title = "Gawr Gura: 2024 Concert Tour", ActivityType = "Article", Action = "Updated", Author = "SharkBite24", Timestamp = DateTime.Now.AddMinutes(-38), Description = "Added full setlist and international ticketing info.", Detail = "+1,420 chars" },
                new Activity { Title = "Hololive Gen 3: Records", ActivityType = "Article", Action = "Updated", Author = "Pekora_Fan_99", Timestamp = DateTime.Now.AddHours(-1), Description = "Corrected date for Usada Pekora's anniversary stream.", Detail = "-12 chars" },
                new Activity { Title = "Talk: Nijisanji EN Graduation", ActivityType = "Community", Action = "Commented", Author = "Mod_Sora", Timestamp = DateTime.Now.AddHours(-3), Description = "Reminder to keep discussion civil and cite official sources.", Detail = "New Comment" },
                new Activity { Title = "Kobo Kanaeru", ActivityType = "Article", Action = "Created", Author = "Raindrops_01", Timestamp = DateTime.Now.AddHours(-5), Description = "Initial page creation for Kobo Kanaeru.", Detail = "New Page" },
                new Activity { Title = "Houshou Marine 1st Album", ActivityType = "Media", Action = "Created", Author = "Ahoy_Captain", Timestamp = DateTime.Now.AddDays(-1), Description = "Uploaded high-resolution cover art.", Detail = "New Image" }
            );
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Seed warning: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

var supportedCultures = new[] { "en", "vi", "ja" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

