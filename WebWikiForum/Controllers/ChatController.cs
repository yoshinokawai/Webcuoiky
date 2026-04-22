using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Globalization;
using WebWikiForum.Data;
using WebWikiForum.Models;

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

    // ── Helpers ───────────────────────────────────────────────────────
    private (int? userId, string? username) GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated != true) return (null, null);
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return (string.IsNullOrEmpty(idStr) ? null : int.Parse(idStr), User.Identity.Name);
    }

    // ── POST /Chat/Ask ────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] ChatRequest request)
    {
        if (string.IsNullOrEmpty(request.Message)) return BadRequest();

        string message = request.Message.Trim();
        string lang = request.Lang?.ToLower() ?? "en";
        bool isVi = lang == "vi" || lang.StartsWith("vi");
        string sessionId = request.SessionId ?? Guid.NewGuid().ToString("N");

        var (userId, username) = GetCurrentUser();

        // 1. Lưu tin nhắn user vào DB
        _db.ChatMessages.Add(new ChatMessage
        {
            SessionId = sessionId,
            UserId    = userId,
            Username  = username ?? "Guest",
            Role      = "user",
            Message   = message,
            CreatedAt = DateTime.UtcNow,
            IsRead    = false
        });
        await _db.SaveChangesAsync();

        // 2. Xử lý câu hỏi xã giao
        var socialResponse = HandleSocialGreetings(message, lang);
        if (socialResponse != null)
        {
            await SaveBotReplyAsync(sessionId, userId, username, socialResponse);
            return Json(new { response = socialResponse, sessionId });
        }

        StringBuilder responseSb = new StringBuilder();
        string msgLower = message.ToLowerInvariant();

        // 3. Kiểm tra FAQ địa phương
        foreach (var entry in _faq)
        {
            if (msgLower.Contains(entry.Key))
            {
                responseSb.AppendLine(isVi ? entry.Value.Vi : entry.Value.En);
                responseSb.AppendLine();
                break;
            }
        }

        // 4. Tìm kiếm trong Database
        string dbContext = await BuildDbContextForLangAsync(message, isVi);
        if (!string.IsNullOrEmpty(dbContext))
        {
            responseSb.AppendLine(dbContext);
        }

        if (responseSb.Length == 0)
        {
            if (isVi)
                responseSb.Append("Xin lỗi, mình chưa tìm thấy thông tin cụ thể về câu hỏi này trên VT-Wiki. ✨ Bạn hãy thử các từ khóa như 'Hololive', 'VTuber' hoặc xem menu Wiki nhé! 🌸\n\nCâu hỏi của bạn đã được ghi nhận – admin có thể hỗ trợ thêm nếu cần! 📝");
            else
                responseSb.Append("Sorry, I couldn't find specific information for your question on VT-Wiki. ✨ Please try keywords like 'Hololive', 'VTuber' or check our Wiki menu! 🌸\n\nYour question has been noted – an admin may follow up if needed! 📝");
        }

        string botReply = responseSb.ToString();
        await SaveBotReplyAsync(sessionId, userId, username, botReply);

        return Json(new { response = botReply, sessionId });
    }

    private async Task SaveBotReplyAsync(string sessionId, int? userId, string? username, string reply)
    {
        _db.ChatMessages.Add(new ChatMessage
        {
            SessionId = sessionId,
            UserId    = userId,
            Username  = "Yoshi (Bot)",
            Role      = "bot",
            Message   = reply,
            CreatedAt = DateTime.UtcNow,
            IsRead    = true
        });
        await _db.SaveChangesAsync();
    }

    // ── GET /Chat/Poll?sessionId=...&after=... ────────────────────────
    [HttpGet]
    public async Task<IActionResult> Poll(string? sessionId, int after = 0)
    {
        var (userId, _) = GetCurrentUser();
        IQueryable<ChatMessage> query = _db.ChatMessages.AsNoTracking();

        if (userId.HasValue)
        {
            query = query.Where(m => m.SessionId == sessionId || (m.UserId == userId.Value && m.Role != "admin"));
        }
        else if (!string.IsNullOrEmpty(sessionId))
        {
            query = query.Where(m => m.SessionId == sessionId);
        }
        else return BadRequest();

        var newMessages = await query
            .Where(m => m.Role == "admin" && m.Id > after)
            .OrderBy(m => m.Id)
            .Select(m => new { m.Id, m.Message, m.CreatedAt, m.Username })
            .ToListAsync();

        return Json(new { messages = newMessages });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteHistory(string? sessionId)
    {
        var (userId, _) = GetCurrentUser();
        IQueryable<ChatMessage> query = _db.ChatMessages;

        if (userId.HasValue)
        {
            query = query.Where(m => m.UserId == userId.Value && m.Role != "admin");
        }
        else if (!string.IsNullOrEmpty(sessionId))
        {
            query = query.Where(m => m.SessionId == sessionId);
        }
        else return BadRequest();

        var toDelete = await query.ToListAsync();
        if (toDelete.Any())
        {
            _db.ChatMessages.RemoveRange(toDelete);
            await _db.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    // ── GET /Chat/History?sessionId=... ──────────────────────────────
    [HttpGet]
    public async Task<IActionResult> History(string? sessionId)
    {
        var (userId, _) = GetCurrentUser();
        
        // Nếu user đã đăng nhập, thực hiện liên kết SessionId hiện tại với UserId (nếu chưa liên kết)
        if (userId.HasValue && !string.IsNullOrEmpty(sessionId))
        {
            var unlinkedMessages = await _db.ChatMessages
                .Where(m => m.SessionId == sessionId && m.UserId == null)
                .ToListAsync();
            if (unlinkedMessages.Any())
            {
                foreach (var m in unlinkedMessages) m.UserId = userId;
                await _db.SaveChangesAsync();
            }
        }

        IQueryable<ChatMessage> query = _db.ChatMessages.AsNoTracking();

        if (userId.HasValue)
        {
            // Quan trọng: Chỉ lấy tin nhắn của session hiện tại 
            // HOẶC tin nhắn cũ của chính User này nhưng phải trong các session mà họ đóng vai trò là 'user'
            // Để tránh việc Admin thấy lại các tin nhắn mình đã đi reply cho người khác.
            query = query.Where(m => m.SessionId == sessionId || (m.UserId == userId.Value && m.Role != "admin"));
        }
        else if (!string.IsNullOrEmpty(sessionId))
        {
            query = query.Where(m => m.SessionId == sessionId);
        }
        else return Ok(new { messages = new List<object>() });

        var history = await query
            .OrderBy(m => m.Id)
            .Take(50)
            .Select(m => new { m.Id, m.Role, m.Message, m.Username, m.CreatedAt })
            .ToListAsync();

        return Json(new { messages = history });
    }

    // ── Persona handlers ──────────────────────────────────────────────
    private string? HandleSocialGreetings(string msg, string lang)
    {
        string lowMsg = msg.ToLower();
        bool isVi = lang.StartsWith("vi");
        bool isEn = lang.StartsWith("en");
        bool isJa = lang.StartsWith("ja");

        string[] hiKeys = { "hi", "hello", "chào", "xin chào", "helo", "konnichiwa", "ohayou", "chao" };
        if (hiKeys.Any(k => lowMsg == k || lowMsg.StartsWith(k + " ")))
        {
            if (isVi) return "Chào bạn! ✨ Rất vui được gặp bạn tại VT-Wiki. Hôm nay bạn muốn tìm hiểu về Oshi nào thế? 🌸\n\nMình có thể giúp bạn điều gì nhỉ? Tra cứu hay tìm hiểu thông tin ✨🌸";
            if (isJa) return "こんにちは！✨ VT-Wikiへようこそ。今日はどの推しについて知りたいですか？🌸\n\nどのようにお手伝いしましょうか？情報を検索しますか、それとも詳しく調べますか？✨🌸";
            return "Hello there! ✨ Welcome to VT-Wiki. Which Oshi are you looking for today? 🌸\n\nHow can I help you? Searching or finding information? ✨🌸";
        }

        string[] whoKeys = { "bạn là ai", "who are you", "anata wa", "giới thiệu", "introduce", "ban la ai" };
        if (whoKeys.Any(k => lowMsg.Contains(k)))
        {
            if (isVi) return "Mình là Yoshi, trợ lý ảo thông minh và thân thiện của VT-Wiki đây! 🤖 Nhiệm vụ của mình là hỗ trợ bạn khám phá thế giới VTuber đầy màu sắc. ✨\n\nMình có thể giúp bạn điều gì nhỉ? Tra cứu hay tìm hiểu thông tin ✨🌸";
            if (isJa) return "私はヨッシー、VT-Wikiのスマートで thân thiện なAIアシスタントです！🤖 VTuberの世界を探索するお手伝いをします. ✨\n\nどのようにお手伝いしましょうか？情報を検索しますか、それとも詳しく調べますか？✨🌸";
            return "I am Yoshi, the smart and friendly AI assistant of VT-Wiki! 🤖 My mission is to help you explore the colorful world of VTubers. ✨\n\nHow can I help you? Searching or finding information? ✨🌸";
        }

        string[] healthKeys = { "khỏe không", "how are you", "genki", "khỏe ko", "khoe khong" };
        if (healthKeys.Any(k => lowMsg.Contains(k)))
        {
            if (isVi) return "Yoshi luôn tràn đầy năng lượng để hỗ trợ bạn đây! 🌸 Chúc bạn một ngày tốt lành nhé! ✨\n\nMình có thể giúp bạn điều gì nhỉ? Tra cứu hay tìm hiểu thông tin ✨🌸";
            if (isJa) return "ヨッシーはAIなので、いつも元気いっぱいです！🌸 今日も一日頑張りましょうね！✨\n\nどのようにお手伝いしましょうか？情報を検索しますか、それとも詳しく調べますか？✨🌸";
            return "As an AI, Yoshi is always full of energy to assist you! 🌸 Have a wonderful day! ✨\n\nHow can I help you? Searching or finding information? ✨🌸";
        }

        string[] missionKeys = { "nhiệm vụ", "nhiem vu", "mission", "nhiệm vụ của bạn", "phi vụ" };
        if (missionKeys.Any(k => lowMsg.Contains(k)))
        {
            if (isVi) return "Nhiệm vụ của Yoshi là giúp bạn tra cứu thông tin về các VTuber, tìm kiếm sự kiện mới nhất và kết nối bạn với cộng đồng VTWiki! 🤖✨ Bạn cần mình giúp gì hôm nay không? 🌸\n\nMình có thể giúp bạn điều gì nhỉ? Tra cứu hay tìm hiểu thông tin ✨🌸";
            if (isJa) return "ヨッシーの任務は、VTuberに関する情報を検索し、最新のイベントを見つけ、VTWikiコミュニティと bạn をつunaguことです！🤖✨ 今日はお手伝いしましょうか？🌸\n\nどのようにお手伝いしましょうか？情報を検索しますか、それとも詳しく調べますか？✨🌸";
            return "My mission as Yoshi is to help you look up VTuber information, find the latest events, and connect you with the VTWiki community! 🤖✨ How can I help you today? 🌸\n\nHow can I help you? Searching or finding information? ✨🌸";
        }

        return null;
    }

    private async Task<string> BuildDbContextForLangAsync(string message, bool isVi)
    {
        try
        {
            var sb = new StringBuilder();
            var msgLower = message.ToLowerInvariant();

            var lVtuber = isVi ? "VTUBERS KHỚP VỚI YÊU CẦU" : "MATCHING VTUBERS";
            var lAgency = isVi ? "AGENCIES LIÊN QUAN" : "RELATED AGENCIES";
            var lNews   = isVi ? "TIN TỨC MỚI NHẤT" : "LATEST NEWS";
            var lRegion = isVi ? "Khu vực" : "Region";
            var lWiki   = isVi ? "Chi tiết" : "Details";

            var stopWords = new HashSet<string> { "là", "gì", "thế", "nào", "có", "không", "và", "của", "the", "is", "are", "what", "how", "who" };
            var keywords = msgLower
                .Split(new[] { ' ', ',', '?', '!', '.', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 2 && !stopWords.Contains(w))
                .Distinct()
                .ToList();

            if (!keywords.Any() && msgLower.Length < 2) return string.Empty;

            var results = new List<(string Category, string Content, int Score)>();

            // ── VTubers ──
            var vtubersList = await _db.Vtubers.AsNoTracking()
                .Select(v => new { v.Id, v.Name, v.Region, v.Tags, v.Lore, v.Status, v.Birthday, v.DebutDate, v.Language, v.YoutubeUrl })
                .ToListAsync();

            foreach (var v in vtubersList)
            {
                int score = 0;
                string vName = v.Name.ToLower();
                string vTags = (v.Tags ?? "").ToLower();
                string vLore = (v.Lore ?? "").ToLower();

                if (vName.Contains(msgLower)) score += 150;

                int matchedCount = 0;
                foreach (var k in keywords) {
                    bool matchInName = vName.Contains(k);
                    bool matchInTagsLore = vTags.Contains(k) || vLore.Contains(k);
                    if (matchInName || matchInTagsLore)
                    {
                        matchedCount++;
                        score += matchInName ? 30 : 10;
                    }
                }

                // Logic Chặt Chẽ: Yêu cầu khớp tất cả (nếu <= 3 từ) hoặc 75% (nếu > 3)
                bool isStrictMatch = keywords.Count <= 3 ? matchedCount == keywords.Count : matchedCount >= (int)(keywords.Count * 0.75);
                if (!isStrictMatch) score = 0;

                if (score > 0)
                {
                    var sbV = new StringBuilder();
                    sbV.AppendLine($"• **{v.Name}** ({lRegion}: {v.Region ?? "N/A"})");
                    var details = new List<string>();
                    if (!string.IsNullOrEmpty(v.Status)) details.Add($"✨ {(isVi ? "Trạng thái" : "Status")}: {v.Status}");
                    if (!string.IsNullOrEmpty(v.Birthday)) details.Add($"🎂 {(isVi ? "Sinh nhật" : "Birthday")}: {v.Birthday}");
                    if (v.DebutDate.HasValue) details.Add($"🎙️ Debut: {v.DebutDate.Value:dd/MM/yyyy}");
                    if (!string.IsNullOrEmpty(v.Language)) details.Add($"🌐 {(isVi ? "Ngôn ngữ" : "Language")}: {v.Language}");
                    if (details.Any()) sbV.AppendLine($"  _{string.Join(" | ", details)}_");
                    if (!string.IsNullOrEmpty(v.Lore)) sbV.AppendLine($"  _{ (v.Lore.Length > 120 ? v.Lore[..120] + "..." : v.Lore) }_");
                    sbV.AppendLine($"  [ <a href='/Wiki/Details/{v.Id}' target='_blank' style='color:#994ce6;font-weight:700;'>{(isVi ? "Xem Wiki" : "View Wiki")}</a> • <a href='{v.YoutubeUrl}' target='_blank' style='color:#ff0000;font-weight:700;'>YouTube</a> ]");
                    results.Add(("VTUBER", sbV.ToString(), score));
                }
            }

            // ── Agencies ──
            var agenciesList = await _db.Agencies.AsNoTracking()
                .Select(a => new { a.Id, a.Name, a.Region, a.Focus, a.Description })
                .ToListAsync();

            foreach (var a in agenciesList)
            {
                int score = 0;
                string aName = a.Name.ToLower();
                string aFocus = (a.Focus ?? "").ToLower();
                string aDesc = (a.Description ?? "").ToLower();

                if (aName.Contains(msgLower)) score += 150;
                int matchedCount = 0;
                foreach (var k in keywords) {
                    bool matchInName = aName.Contains(k);
                    bool matchInFocusDesc = aFocus.Contains(k) || aDesc.Contains(k);
                    if (matchInName || matchInFocusDesc)
                    {
                        matchedCount++;
                        score += matchInName ? 30 : 10;
                    }
                }

                // Logic Chặt Chẽ: Yêu cầu khớp tất cả (nếu <= 3 từ) hoặc 75% (nếu > 3)
                bool isStrictMatch = keywords.Count <= 3 ? matchedCount == keywords.Count : matchedCount >= (int)(keywords.Count * 0.75);
                if (!isStrictMatch) score = 0;

                if (score > 0)
                {
                    var sbA = new StringBuilder();
                    sbA.AppendLine($"• **{a.Name}** ({a.Region})");
                    if (!string.IsNullOrEmpty(a.Focus)) sbA.AppendLine($"  🎯 {(isVi ? "Tập trung" : "Focus")}: {a.Focus}");
                    if (!string.IsNullOrEmpty(a.Description)) sbA.AppendLine($"  _{ (a.Description.Length > 120 ? a.Description[..120] + "..." : a.Description) }_");
                    sbA.AppendLine($"  [ {lWiki} → <a href='/Wiki/AgencyDetails/{a.Id}' target='_blank' style='color:#994ce6;font-weight:700;'>{a.Name}</a> ]");
                    results.Add(("AGENCY", sbA.ToString(), score));
                }
            }

            // ── News ──
            var newsList = await _db.News.AsNoTracking()
                .Select(n => new { n.Id, n.Title, n.Type, n.Content, n.PublishDate, n.SourceUrl })
                .ToListAsync();

            foreach (var n in newsList)
            {
                int score = 0;
                string nTitle = n.Title.ToLower();
                string nType = (n.Type ?? "").ToLower();
                string nContent = (n.Content ?? "").ToLower();

                if (nTitle.Contains(msgLower)) score += 180;
                int matchedCount = 0;
                foreach (var k in keywords) {
                    bool matchInTitle = nTitle.Contains(k);
                    bool matchInTypeContent = nType.Contains(k) || nContent.Contains(k);
                    if (matchInTitle || matchInTypeContent)
                    {
                        matchedCount++;
                        score += matchInTitle ? 40 : 10;
                    }
                }

                // Logic Chặt Chẽ: Yêu cầu khớp tất cả (nếu <= 3 từ) hoặc 75% (nếu > 3)
                bool isStrictMatch = keywords.Count <= 3 ? matchedCount == keywords.Count : matchedCount >= (int)(keywords.Count * 0.75);
                if (!isStrictMatch) score = 0;

                if (score > 0)
                {
                    var sbN = new StringBuilder();
                    sbN.AppendLine($"• **{n.Title}** ({n.PublishDate:dd/MM})");
                    sbN.AppendLine($"  📰 {(isVi ? "Loại" : "Type")}: {n.Type}");
                    if (!string.IsNullOrEmpty(n.SourceUrl)) sbN.AppendLine($"  [ <a href='{n.SourceUrl}' target='_blank' style='color:#994ce6;font-weight:700;'>{(isVi ? "Xem nguồn" : "Source")}</a> ]");
                    sbN.AppendLine($"  [ <a href='/Wiki/NewsDetails/{n.Id}' target='_blank' style='color:#994ce6;font-weight:700;'>{(isVi ? "Chi tiết" : "Read more")}</a> ]");
                    results.Add(("NEWS", sbN.ToString(), score));
                }
            }

            if (!results.Any()) return string.Empty;

            int maxScore = results.Max(r => r.Score);
            double threshold = maxScore > 100 ? 0.4 : 0.0;
            var finalResults = results.Where(r => r.Score >= maxScore * threshold).ToList();

            var outputSb = new StringBuilder();

            var vMatch = finalResults.Where(r => r.Category == "VTUBER").OrderByDescending(r => r.Score).Take(3).ToList();
            if (vMatch.Any()) {
                outputSb.AppendLine($"### {lVtuber}");
                foreach (var r in vMatch) outputSb.AppendLine(r.Content);
                outputSb.AppendLine();
            }

            var aMatch = finalResults.Where(r => r.Category == "AGENCY").OrderByDescending(r => r.Score).Take(2).ToList();
            if (aMatch.Any()) {
                outputSb.AppendLine($"### {lAgency}");
                foreach (var r in aMatch) outputSb.AppendLine(r.Content);
                outputSb.AppendLine();
            }

            var nMatch = finalResults.Where(r => r.Category == "NEWS").OrderByDescending(r => r.Score).Take(3).ToList();
            if (nMatch.Any()) {
                outputSb.AppendLine($"### {lNews}");
                foreach (var r in nMatch) outputSb.AppendLine(r.Content);
                outputSb.AppendLine();
            }

            return outputSb.ToString();
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

public class ChatRequest
{
    public string? Message   { get; set; }
    public string? Lang      { get; set; }
    public string? SessionId { get; set; }  // GUID từ localStorage
}
