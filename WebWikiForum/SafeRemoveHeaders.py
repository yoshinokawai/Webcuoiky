import os

views_dir = r"d:\Code\Webcuoiky\WebWikiForum\Views"
exclude_files = ["_Layout.cshtml", "_ViewStart.cshtml"]
standalone_files = ["Login.cshtml", "CreateAccount.cshtml"] # these shouldn't inherit the global navigation menu

for root, dirs, files in os.walk(views_dir):
    for filename in files:
        if not filename.endswith(".cshtml") or filename in exclude_files:
            continue
            
        filepath = os.path.join(root, filename)
        with open(filepath, "r", encoding="utf-8") as f:
            content = f.read()
            
        # Disable Layout for Login/CreateAccount so they are fullscreen layouts
        if filename in standalone_files:
            if "@{ Layout = null; }" not in content:
                content = "@{ Layout = null; }\n" + content
            with open(filepath, "w", encoding="utf-8") as f:
                f.write(content)
            continue
            
        # Extract the <header> tag safely by finding boundaries
        start_idx = content.find("<header")
        if start_idx != -1:
            end_idx = content.find("</header>", start_idx)
            if end_idx != -1:
                end_idx += len("</header>")
                content = content[:start_idx] + content[end_idx:]
                
        # Extract the <footer> tag safely
        start_idx = content.find("<footer")
        if start_idx != -1:
            end_idx = content.find("</footer>", start_idx)
            if end_idx != -1:
                end_idx += len("</footer>")
                content = content[:start_idx] + content[end_idx:]
                
        with open(filepath, "w", encoding="utf-8") as f:
            f.write(content)

print("Safe header removal complete!")
