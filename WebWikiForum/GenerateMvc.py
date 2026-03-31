import os
import shutil

raw_dir = r"d:\Code\Webcuoiky\WebWikiForum\RawScreens"
project_dir = r"d:\Code\Webcuoiky\WebWikiForum"
views_dir = os.path.join(project_dir, "Views")
css_dir = os.path.join(project_dir, "wwwroot", "css")
controllers_dir = os.path.join(project_dir, "Controllers")

mapping = [
    ("Home", "Index", "VTuberWikiHomePagewithNavigationDropdowns"),
    ("Home", "Explore", "ExploreUpdatedNav"),
    ("Home", "AboutUs", "AboutUsNavigationFix"),
    ("Home", "RecentChanges", "RecentChanges"),
    ("Home", "JoinDiscord", "JoinourDiscord"),
    ("Account", "Login", "VTuberWikiLoginVariant2"),
    ("Account", "CreateAccount", "VTuberWikiCreateAccount"),
    ("Wiki", "Agencies", "AgenciesOverviewBackgroundUpdated"),
    ("Wiki", "Independent", "IndependentVTubersFinalNavigationFix"),
    ("Wiki", "Translation", "TranslationProject"),
    ("Forum", "Community", "CommunityForumUpdatedNav"),
    ("Forum", "WikiForum", "WikiForumNavigationFix"),
    ("Help", "HelpCenter", "HelpCenterNavigationFixedFinal"),
    ("Help", "Guidelines", "GuidelinesNavigationFix"),
    ("Editor", "EditorHub", "EditorHubNavigationFix"),
    ("Editor", "FanTools", "FanToolsUpdatedNavigationDropdowns")
]

controller_template = """using Microsoft.AspNetCore.Mvc;

namespace WebWikiForum.Controllers
{{
    public class {0}Controller : Controller
    {{
{1}
    }}
}}
"""
action_template = """        public IActionResult {0}()
        {{
            return View();
        }}
"""

controllers = {}

for folder, action, source in mapping:
    view_folder = os.path.join(views_dir, folder)
    os.makedirs(view_folder, exist_ok=True)
    
    src_cshtml = os.path.join(raw_dir, f"{source}.cshtml")
    dst_cshtml = os.path.join(view_folder, f"{action}.cshtml")
    if os.path.exists(src_cshtml):
        with open(src_cshtml, "r", encoding="utf-8") as f:
            content = f.read()
        content = content.replace(f"~/css/{source}.css", f"~/css/{action}.css")
        with open(dst_cshtml, "w", encoding="utf-8") as f:
            f.write(content)
            
    src_css = os.path.join(raw_dir, f"{source}.css")
    dst_css = os.path.join(css_dir, f"{action}.css")
    if os.path.exists(src_css):
        shutil.copy(src_css, dst_css)
        
    if folder not in controllers:
        controllers[folder] = []
    controllers[folder].append(action_template.format(action))
    
for folder, actions in controllers.items():
    dst_ctrl = os.path.join(controllers_dir, f"{folder}Controller.cs")
    with open(dst_ctrl, "w", encoding="utf-8") as f:
        f.write(controller_template.format(folder, "".join(actions)))

print("Finished mapping logic into ASP.NET Core MVC project structure.")
