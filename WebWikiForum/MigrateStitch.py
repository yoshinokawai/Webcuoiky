import json
import urllib.request
import re
import os

json_path = r"C:\Users\fuwamococoa\.gemini\antigravity\brain\0869f833-1ae2-4d76-8a11-b8266d116661\.system_generated\steps\60\output.txt"
with open(json_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

output_dir = r"D:\Code\Webcuoiky\WebWikiForum\RawScreens"
os.makedirs(output_dir, exist_ok=True)

for screen in data.get('screens', []):
    title = screen.get('title', 'Unknown').replace(' - VTuber Wiki', '').replace(' ', '')
    title = re.sub(r'[^A-Za-z0-9]', '', title)
    
    if 'htmlCode' in screen and 'downloadUrl' in screen['htmlCode']:
        url = screen['htmlCode']['downloadUrl']
        print(f"Fetching {title}...")
        try:
            req = urllib.request.urlopen(url)
            html_content = req.read().decode('utf-8')
            
            style_match = re.search(r'<style>(.*?)</style>', html_content, re.DOTALL | re.IGNORECASE)
            style_content = style_match.group(1) if style_match else ""
            
            body_match = re.search(r'<body[^>]*>(.*?)</body>', html_content, re.DOTALL | re.IGNORECASE)
            body_content = body_match.group(1) if body_match else html_content
            
            # Just fixing image paths to avoid 404s dynamically if they exist, but for now exact copy
            with open(os.path.join(output_dir, f"{title}.css"), 'w', encoding='utf-8') as f:
                f.write(style_content)
                
            with open(os.path.join(output_dir, f"{title}.cshtml"), 'w', encoding='utf-8') as f:
                # Add section style
                f.write(f'@section Styles {{\n    <link rel="stylesheet" href="~/css/{title}.css" />\n}}\n\n')
                f.write(body_content)
            
            print(f" => Successfully processed {title}")
        except Exception as e:
            print(f" => Failed to process {title}: {e}")
