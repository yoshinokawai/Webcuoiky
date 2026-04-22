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
                new Agency { Name = "VShojo", LogoUrl = "https://upload.wikimedia.org/wikipedia/en/thumb/0/07/VShojo_logo.png/250px-VShojo_logo.png", Region = "United States", Focus = "Streamer-led", Description = "A talent-first agency focusing on creator freedom and IP ownership.", TalentCount = 14 }
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

        // ===== ONE-TIME REGION STANDARDIZATION =====
        var allAgencies = await db.Agencies.ToListAsync();
        foreach (var agency in allAgencies)
        {
            if (agency.Region == "US" || agency.Region == "NA" || agency.Region == "North America")
            {
                agency.Region = "United States";
            }
            else if (agency.Region == "CN")
            {
                agency.Region = "China";
            }
            else if (agency.Region == "SEA" || agency.Region == "Europe" || agency.Region == "Other" || agency.Region == "World" || string.IsNullOrEmpty(agency.Region))
            {
                if (agency.Region != "Japan" && agency.Region != "Vietnam" && agency.Region != "Indonesia" && agency.Region != "China" && agency.Region != "United States")
                {
                    agency.Region = "Global";
                }
            }
        }

        var allVtubers = await db.Vtubers.ToListAsync();
        foreach (var vtuber in allVtubers)
        {
            if (vtuber.Region == "US" || vtuber.Region == "NA" || vtuber.Region == "North America")
            {
                vtuber.Region = "United States";
            }
            else if (vtuber.Region == "CN")
            {
                vtuber.Region = "China";
            }
            else if (vtuber.Region == "SEA" || vtuber.Region == "Europe" || vtuber.Region == "Other" || vtuber.Region == "World" || string.IsNullOrEmpty(vtuber.Region))
            {
                if (vtuber.Region != "Japan" && vtuber.Region != "Vietnam" && vtuber.Region != "Indonesia" && vtuber.Region != "China" && vtuber.Region != "United States")
                {
                    vtuber.Region = "Global";
                }
            }
        }
        await db.SaveChangesAsync();
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

