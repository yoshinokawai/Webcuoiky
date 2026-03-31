import os
import re

out_html_path = r'd:\Code\Webcuoiky\WebWikiForum\out.html'
layout_path = r'd:\Code\Webcuoiky\WebWikiForum\Views\Shared\_Layout.cshtml'
views_dir = r'd:\Code\Webcuoiky\WebWikiForum\Views'

with open(out_html_path, 'r', encoding='utf-8') as f:
    html = f.read()

# Extract header and footer
header_match = re.search(r'(<header.*?</header>)', html, re.DOTALL | re.IGNORECASE)
footer_match = re.search(r'(<footer.*?</footer>)', html, re.DOTALL | re.IGNORECASE)

if not header_match or not footer_match:
    print("Could not find header or footer in out.html")
    exit(1)

header = header_match.group(1)
footer = footer_match.group(1)

# Mapping of link text to href
link_mapping = {
    'Home': '/Home/Index',
    'Agencies': '/Wiki/Agencies',
    'Independent': '/Wiki/Independent',
    'Indie VTubers': '/Wiki/Independent',
    'Fan Tools': '/Editor/FanTools',
    'Recent Changes': '/Home/RecentChanges',
    'Guidelines': '/Help/Guidelines',
    'Editors Hub': '/Editor/EditorHub',
    'Wiki Forum': '/Forum/WikiForum',
    'Discord Link': '/Home/JoinDiscord',
    'About Us': '/Home/AboutUs',
    'Translation Project': '/Wiki/Translation',
    'Help Center': '/Help/HelpCenter',
    'Log In': '/Account/Login',
    'Sign Up': '/Account/CreateAccount'
}

# Replace links in header and footer
for text, href in link_mapping.items():
    # Replace buttons text exact match or inner text
    # e.g. <a href="#">Home</a> => <a href="/Home/Index">Home</a>
    header = re.sub(rf'<a([^>]*)href="#"([^>]*)>([^<]*){text}([^<]*)</a>', rf'<a\1href="{href}"\2>\3{text}\4</a>', header, flags=re.IGNORECASE)
    footer = re.sub(rf'<a([^>]*)href="#"([^>]*)>([^<]*){text}([^<]*)</a>', rf'<a\1href="{href}"\2>\3{text}\4</a>', footer, flags=re.IGNORECASE)
    
    # Check for buttons too (like Login and Sign up)
    header = re.sub(rf'<button([^>]*)>([^<]*){text}([^<]*)</button>', rf'<a href="{href}"\1 style="display:inline-flex">\2{text}\3</a>', header, flags=re.IGNORECASE)
    footer = re.sub(rf'<button([^>]*)>([^<]*){text}([^<]*)</button>', rf'<a href="{href}"\1 style="display:inline-flex">\2{text}\3</a>', footer, flags=re.IGNORECASE)

# Ensure "VTWiki" logo links back to Home
header = re.sub(r'<div class="flex items-center gap-2 flex-shrink-0">', r'<a href="/Home/Index" class="flex items-center gap-2 flex-shrink-0">', header)
header = header.replace('<span class="text-xl font-black tracking-tight text-slate-900 dark:text-white">VTWiki</span>\n</div>', '<span class="text-xl font-black tracking-tight text-slate-900 dark:text-white">VTWiki</span>\n</a>')

footer = re.sub(r'<div class="flex items-center gap-2 mb-4">', r'<a href="/Home/Index" class="flex items-center gap-2 mb-4">', footer)
footer = footer.replace('<span class="text-lg font-bold text-slate-900 dark:text-white">VTWiki</span>\n</div>', '<span class="text-lg font-bold text-slate-900 dark:text-white">VTWiki</span>\n</a>')

# Let's insert into _Layout.cshtml
with open(layout_path, 'r', encoding='utf-8') as f:
    layout_content = f.read()

# Un-escape Razor escaping @@ so it compiles smoothly if any existed
header = header.replace('@@', '@')
footer = footer.replace('@@', '@')

# Insert around @RenderBody()
if '@RenderBody()' in layout_content and '<header' not in layout_content:
    layout_content = layout_content.replace('@RenderBody()', f'{header}\n    @RenderBody()\n    {footer}')

with open(layout_path, 'w', encoding='utf-8') as f:
    f.write(layout_content)

# Strip Headers/Footers from all individual views
for root, dirs, files in os.walk(views_dir):
    for filename in files:
        if filename.endswith('.cshtml') and filename != '_Layout.cshtml' and filename != '_ViewStart.cshtml':
            filepath = os.path.join(root, filename)
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Remove <header>...</header>
            content = re.sub(r'<header.*?</header>', '', content, flags=re.DOTALL | re.IGNORECASE)
            # Remove <footer>...</footer>
            content = re.sub(r'<footer.*?</footer>', '', content, flags=re.DOTALL | re.IGNORECASE)
            
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)

print("Migration successful")
