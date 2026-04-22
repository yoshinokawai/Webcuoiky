import xml.etree.ElementTree as ET
import os

def clean_resx(file_path):
    if not os.path.exists(file_path):
        print(f"File not found: {file_path}")
        return

    print(f"Cleaning {file_path}...")
    
    # Parse the XML
    tree = ET.parse(file_path)
    root = tree.getroot()

    # Dictionary to keep track of seen keys
    seen_keys = set()
    to_remove = []

    # Find all 'data' elements
    for data in root.findall('data'):
        name = data.get('name')
        if name in seen_keys:
            print(f"  Removing duplicate key: {name}")
            to_remove.append(data)
        else:
            seen_keys.add(name)

    # Remove duplicates
    for data in to_remove:
        root.remove(data)

    # Write back
    tree.write(file_path, encoding='utf-8', xml_declaration=True)
    print(f"Finished cleaning {file_path}. Total keys: {len(seen_keys)}")

# Paths to the resx files
base_path = r'd:\Code\Webcuoiky\WebWikiForum\Resources'
clean_resx(os.path.join(base_path, 'SharedResource.en.resx'))
clean_resx(os.path.join(base_path, 'SharedResource.vi.resx'))
