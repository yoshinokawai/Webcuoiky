using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebWikiForum.Data;
using WebWikiForum.Models;
using WebWikiForum.Services;
using Microsoft.Extensions.Options;
using WebWikiForum;
using Microsoft.EntityFrameworkCore.Diagnostics;

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
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    // Tắt cảnh báo Model thay đổi để tránh lỗi khi chạy trên Somee sau khi đã update SQL thủ công
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddHttpClient(); // For AI Chat proxy

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
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

        // Nạp thêm VTubers mẫu nếu chưa có (Đảm bảo có Miko và Gura để test)
        if (!db.Vtubers.Any(v => v.Name.Contains("Sakura Miko")))
        {
            db.Vtubers.Add(new Vtuber { Name = "Sakura Miko", DebutDate = new DateTime(2018, 8, 1), Birthday = "August 1", AvatarUrl = "https://yt3.googleusercontent.com/ytc/AIdro_nNf1N1qE-S7mXvK-jZ6b1z9Z2_r7nZ9mZ0zZ0_3A=s176-c-k-c0x00ffffff-no-rj", Status = "Approved", Language = "Japanese", Region = "Japan", Tags = "Gaming, Elite", IsIndependent = false, AgencyId = 1, Lore = "Elite Miko of Hololive Generation 0. Known for her unique way of speaking and chaotic gaming sessions.", YoutubeUrl = "https://www.youtube.com/@SakuraMiko" });
        }
        if (!db.Vtubers.Any(v => v.Name.Contains("Gawr Gura")))
        {
            db.Vtubers.Add(new Vtuber { Name = "Gawr Gura", DebutDate = new DateTime(2020, 9, 13), Birthday = "June 20", AvatarUrl = "https://yt3.googleusercontent.com/ytc/AIdro_l-T-T3_n-U_v-F_B_P_a_J_f_x_G_i_J_f_x_G_i=s176-c-k-c0x00ffffff-no-rj", Status = "Approved", Language = "English", Region = "EN", Tags = "Gaming, Shark", IsIndependent = false, AgencyId = 1, Lore = "A shark VTuber from Hololive English -Myth-. The most subscribed VTuber globally.", YoutubeUrl = "https://www.youtube.com/@GawrGura" });
        }
        db.SaveChanges();

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

        // ===== SEED ADMIN ACCOUNTS =====
        var adminAccounts = new[]
        {
            new { Username = "Yoshino",  Email = "yoshino@vtwiki.com",  Password = "12345" },
            new { Username = "Loc123",   Email = "loc123@vtwiki.com",   Password = "12345" },
            new { Username = "QuocAnh", Email = "quocanh@vtwiki.com",  Password = "12345" },
        };

        foreach (var admin in adminAccounts)
        {
            if (!db.Users.Any(u => u.Username == admin.Username))
            {
                db.Users.Add(new WebWikiForum.Models.User
                {
                    Username     = admin.Username,
                    Email        = admin.Email,
                    PasswordHash = SeedHashPassword(admin.Password),
                    Role         = "Admin",
                    CreatedAt    = DateTime.UtcNow
                });
            }
            else
            {
                // Promote existing account to Admin
                var existing = db.Users.First(u => u.Username == admin.Username);
                if (existing.Role != "Admin")
                {
                    existing.Role = "Admin";
                    db.Users.Update(existing);
                }
            }
        }
        db.SaveChanges();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Seed warning: {ex.Message}");
    }
}

// Helper: same hash algorithm as AccountController
static string SeedHashPassword(string password)
{
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    var saltedPassword = "VTWiki_Salt_" + password;
    var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(saltedPassword));
    return Convert.ToBase64String(bytes);
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

