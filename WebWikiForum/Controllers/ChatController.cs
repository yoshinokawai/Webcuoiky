using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Globalization;
using WebWikiForum.Data;

namespace WebWikiForum.Controllers;

public class ChatController : Controller
{
    private readonly ApplicationDbContext _db;

    public ChatController(ApplicationDbContext db)
    {
        _db = db;
    }

    // ── Local FAQ Knowledge Base ──────────────────────────────────────
    private static readonly Dictionary<string, (string En, string Vi)> _faq = new(StringComparer.OrdinalIgnoreCase)
    {
        { "vtuber", (
            "A VTuber (Virtual YouTuber) is an online entertainer who uses a digital avatar (usually anime-style) created with 2D or 3D graphics.",
            "VTuber (Virtual YouTuber) là những người sáng tạo nội dung trực tuyến sử dụng nhân vật ảo (thường mang phong cách anime) được tạo ra bằng đồ họa 2D hoặc 3D."
        )},
        { "agency", (
            "VTuber Agencies are companies that manage and support multiple VTubers, providing them with technical equipment, marketing, and professional opportunities (e.g., Hololive, NIJISANJI).",
            "Agency VTuber là các công ty quản lý và hỗ trợ nhiều VTuber, cung cấp cho họ thiết bị kỹ thuật, truyền thông và các cơ hội chuyên nghiệp (ví dụ: Hololive, NIJISANJI)."
        )},
        { "vtwiki", (
            "VTWiki is a community-driven encyclopedia dedicated to VTubers, agencies, and virtual streaming culture. You can browse information, join the forum, and contribute by registering an account.",
            "VTWiki là một bách khoa toàn thư do cộng đồng xây dựng, chuyên về VTubers, các agency và văn hóa livestream ảo. Bạn có thể tra cứu thông tin, tham gia diễn đàn và đóng góp nội dung bằng cách đăng ký tài khoản."
        )},
        { "how to use", (
            "You can use the 'Explore' menu to find VTubers, 'Agencies' to see groups, and 'News' for latest updates. Register an account to join the Forum and edit Wiki pages!",
            "Bạn có thể dùng menu 'Khám phá' để tìm VTubers, 'Agencies' để xem các nhóm, và 'Tin tức' để cập nhật sự kiện mới nhất. Hãy đăng ký tài khoản để tham gia Diễn đàn và chỉnh sửa trang Wiki nhé!"
        )},
        { "admin", (
            "The VTWiki project was created by Yoshi, Loc123, and QuocAnh as a final project for web development.",
            "Dự án VTWiki được phát triển bởi Yoshi, Loc123, và QuocAnh như một đồ án cuối kỳ về phát triển ứng dụng web."
        )}
    };

    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Message))
            return BadRequest(new { error = "Message cannot be empty." });

        var msgLower = request.Message.ToLowerInvariant();
        var isVi = CultureInfo.CurrentUICulture.Name.StartsWith("vi");
        
        StringBuilder responseSb = new StringBuilder();

        // 1. Kiểm tra FAQ địa phương (Keyword matching)
        foreach (var entry in _faq)
        {
            if (msgLower.Contains(entry.Key))
            {
                responseSb.AppendLine(isVi ? entry.Value.Vi : entry.Value.En);
                responseSb.AppendLine();
                break; // Tìm thấy 1 cái chính là đủ
            }
        }

        // 2. Tìm kiếm trong Database (VTubers, Agencies, News)
        var dbContext = await BuildDbContextForLangAsync(request.Message, isVi);
        if (!string.IsNullOrEmpty(dbContext))
        {
            responseSb.AppendLine(dbContext);
        }

        // 3. Fallback nếu không có gì
        if (responseSb.Length == 0)
        {
            if (isVi)
                responseSb.Append("Xin lỗi, tôi chưa tìm thấy thông tin cụ thể về câu hỏi này trên VTWiki. Bạn hãy thử các từ khóa như 'Hololive', 'VTuber' hoặc xem menu Wiki nhé!");
            else
                responseSb.Append("Sorry, I couldn't find specific information for your question on VTWiki. Please try keywords like 'Hololive', 'VTuber' or check our Wiki menu!");
        }

        return Ok(new { reply = responseSb.ToString() });
    }

    private async Task<string> BuildDbContextForLangAsync(string message, bool isVi)
    {
        try
        {
            var sb = new StringBuilder();
            var msgLower = message.ToLowerInvariant();
            
            // Labels theo ngôn ngữ
            var lVtuber = isVi ? "VTUBERS KHỚP VỚI YÊU CẦU" : "MATCHING VTUBERS";
            var lAgency = isVi ? "AGENCIES LIÊN QUAN" : "RELATED AGENCIES";
            var lNews   = isVi ? "TIN TỨC MỚI NHẤT" : "LATEST NEWS";
            var lName   = isVi ? "Tên" : "Name";
            var lRegion = isVi ? "Khu vực" : "Region";
            var lAgencyLabel = isVi ? "Công ty" : "Agency";
            var lDesc   = isVi ? "Mô tả" : "Description";
            var lWiki   = isVi ? "Chi tiết" : "Details";

            var stopWords = new HashSet<string> { "là", "gì", "thế", "nào", "có", "không", "và", "của", "the", "is", "are", "what", "how", "who" };
            var keywords = msgLower
                .Split(new[] { ' ', ',', '?', '!', '.', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 2 && !stopWords.Contains(w)) // Giảm xuống 2 để bắt được các từ ngắn
                .Distinct()
                .ToList();

            if (!keywords.Any() && msgLower.Length < 2) return string.Empty;

            // ── VTubers ──
            var vtubers = await _db.Vtubers
                .AsNoTracking()
                .Select(v => new { v.Id, v.Name, v.Region, v.Tags, v.Lore, v.AgencyId, v.Birthday, v.DebutDate, v.Language, v.YoutubeUrl, v.Status })
                .ToListAsync();

            // Tìm kiếm linh hoạt hơn
            var matchedV = vtubers.Where(v => 
                v.Name.Contains(message, StringComparison.OrdinalIgnoreCase) || 
                keywords.Any(k => v.Name.Contains(k, StringComparison.OrdinalIgnoreCase) || (v.Tags??"").Contains(k, StringComparison.OrdinalIgnoreCase))
            ).Take(3).ToList();

            if (matchedV.Any())
            {
                sb.AppendLine($"### {lVtuber}");
                foreach (var v in matchedV)
                {
                    sb.AppendLine($"• **{v.Name}** ({lRegion}: {v.Region ?? "N/A"})");
                    
                    var details = new List<string>();
                    if (!string.IsNullOrEmpty(v.Status)) details.Add($"✨ {(isVi ? "Trạng thái" : "Status")}: {v.Status}");
                    if (!string.IsNullOrEmpty(v.Birthday)) details.Add($"🎂 {(isVi ? "Sinh nhật" : "Birthday")}: {v.Birthday}");
                    if (v.DebutDate.HasValue) details.Add($"🎙️ Debut: {v.DebutDate.Value:dd/MM/yyyy}");
                    if (!string.IsNullOrEmpty(v.Language)) details.Add($"🌐 {(isVi ? "Ngôn ngữ" : "Language")}: {v.Language}");
                    
                    if (details.Any()) sb.AppendLine($"  _{string.Join(" | ", details)}_");

                    if (!string.IsNullOrEmpty(v.Lore)) 
                    {
                        var snippet = v.Lore.Length > 120 ? v.Lore[..120] + "..." : v.Lore;
                        sb.AppendLine($"  _{snippet}_");
                    }

                    var links = new List<string>();
                    links.Add($"<a href='/Wiki/Details/{v.Id}' target='_blank' style='color:#994ce6;font-weight:700;'>{(isVi ? "Xem Wiki" : "View Wiki")}</a>");
                    if (!string.IsNullOrEmpty(v.YoutubeUrl)) links.Add($"<a href='{v.YoutubeUrl}' target='_blank' style='color:#ff0000;font-weight:700;'>YouTube</a>");
                    
                    sb.AppendLine($"  [ {string.Join(" • ", links)} ]");
                }
                sb.AppendLine();
            }

            // ── Agencies ──
            var agencies = await _db.Agencies.AsNoTracking()
                .Select(a => new { a.Id, a.Name, a.Region, a.Focus, a.Description })
                .ToListAsync();

            var matchedA = agencies.Where(a => 
                a.Name.Contains(message, StringComparison.OrdinalIgnoreCase) ||
                keywords.Any(k => 
                    a.Name.Contains(k, StringComparison.OrdinalIgnoreCase) || 
                    (a.Focus ?? "").Contains(k, StringComparison.OrdinalIgnoreCase) ||
                    (a.Description ?? "").Contains(k, StringComparison.OrdinalIgnoreCase)
                )
            ).Take(2).ToList();

            if (matchedA.Any())
            {
                sb.AppendLine($"### {lAgency}");
                foreach (var a in matchedA)
                {
                    sb.AppendLine($"• **{a.Name}** ({a.Region})");
                    if (!string.IsNullOrEmpty(a.Focus)) sb.AppendLine($"  🎯 {(isVi ? "Tập trung" : "Focus")}: {a.Focus}");
                    if (!string.IsNullOrEmpty(a.Description))
                    {
                        var snippet = a.Description.Length > 120 ? a.Description[..120] + "..." : a.Description;
                        sb.AppendLine($"  _{snippet}_");
                    }
                    sb.AppendLine($"  [ {lWiki} → <a href='/Wiki/AgencyDetails/{a.Id}' target='_blank' style='color:#994ce6;font-weight:700;'>{a.Name}</a> ]");
                }
                sb.AppendLine();
            }

            // ── News ──
            var newsItems = await _db.News.AsNoTracking()
                .Select(n => new { n.Id, n.Title, n.Type, n.Content, n.PublishDate, n.SourceUrl })
                .ToListAsync();

            var matchedN = newsItems.Where(n => 
                n.Title.Contains(message, StringComparison.OrdinalIgnoreCase) ||
                keywords.Any(k => 
                    n.Title.Contains(k, StringComparison.OrdinalIgnoreCase) || 
                    (n.Type ?? "").Contains(k, StringComparison.OrdinalIgnoreCase) ||
                    (n.Content ?? "").Contains(k, StringComparison.OrdinalIgnoreCase)
                )
            ).OrderByDescending(n => n.PublishDate).Take(3).ToList();

            if (matchedN.Any())
            {
                sb.AppendLine($"### {lNews}");
                foreach (var n in matchedN)
                {
                    sb.AppendLine($"• **{n.Title}** ({n.PublishDate:dd/MM})");
                    var typeLabel = isVi ? "Loại" : "Type";
                    sb.AppendLine($"  📰 {typeLabel}: {n.Type}");
                    
                    if (!string.IsNullOrEmpty(n.SourceUrl))
                    {
                        var linkText = isVi ? "Xem nguồn tin" : "Source Link";
                        sb.AppendLine($"  [ <a href='{n.SourceUrl}' target='_blank' style='color:#994ce6;font-weight:700;'>{linkText}</a> ]");
                    }

                    var localLinkText = isVi ? "Xem chi tiết" : "Read more";
                    sb.AppendLine($"  [ <a href='/Wiki/NewsDetails/{n.Id}' target='_blank' style='color:#994ce6;font-weight:700;'>{localLinkText}</a> ]");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch { return string.Empty; }
    }

    [HttpGet]
    public async Task<IActionResult> GetUserAvatar()
    {
        if (User.Identity?.IsAuthenticated != true) return Ok(new { avatarUrl = "", username = "" });
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Ok(new { avatarUrl = "", username = User.Identity.Name });

        var user = await _db.Users.AsNoTracking().Where(u => u.Id == int.Parse(userId))
            .Select(u => new { u.AvatarUrl, u.Username }).FirstOrDefaultAsync();
        return Ok(new { avatarUrl = user?.AvatarUrl ?? "", username = user?.Username ?? User.Identity.Name });
    }
}

public class ChatRequest { public string? Message { get; set; } }
