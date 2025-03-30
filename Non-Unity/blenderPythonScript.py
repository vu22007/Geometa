import bpy
import sys
import os

# Get the directory where the script is running
script_dir = os.path.dirname(os.path.realpath(__file__))
# Filepath where the file will be exported
output_path = os.path.normpath(os.path.join(script_dir, "..\\Assets\\Resources\\Prefabs\\Map\\Buildify3DBuildings.fbx"))
# Path to buildify file 
buildifyPath = os.path.normpath(os.path.join(script_dir, "buildify_1.0.blend"))
# Path to BLOSM 
blosm_addon_path = os.path.normpath(os.path.join(script_dir, "blosm_2.7.13.zip"))

# File for storing the osm files
osm_files_dir = os.path.join(script_dir, "osm_files")
if not os.path.exists(osm_files_dir):
    os.makedirs(osm_files_dir)
    print(f"Created OSM files directory: {osm_files_dir}")

# Install the add on if not already installed
def ensure_blosm_installed(addon_path):
    if "blosm" in bpy.context.preferences.addons:
        print("BLOSM is already installed and enabled")
        return True
    
    print(f"Installing BLOSM add-on from {addon_path}...")
    try:
        # Install and enable
        bpy.ops.preferences.addon_install(filepath=addon_path)
        bpy.ops.preferences.addon_enable(module="blosm")
        
        print("BLOSM add-on installed successfully")
        return True
    except Exception as e:
        print(f"Failed to install BLOSM: {e}")
        return False

ensure_blosm_installed(blosm_addon_path)

# Clear all objects
for obj in bpy.data.objects:
    bpy.data.objects.remove(obj, do_unlink=True)

argv = sys.argv
if "--" in argv:
    idx = argv.index("--")
    script_args = argv[idx+1:]
else:
    script_args = []

# Default to file mode if no arguments are provided
if not script_args:
    print("No arguments provided, using default file mode")
    source_type = "file"
else:
    source_type = script_args[0].lower()
    print(f"Source type: {source_type}")
    print(f"All arguments: {script_args}")

blosm_props = bpy.context.scene.blosm
bpy.context.preferences.addons["blosm"].preferences.dataDir = osm_files_dir
blosm_props.mode = "2D"
blosm_props.buildings = True   # Import just buildings
blosm_props.water = False      
blosm_props.forests = False    
blosm_props.vegetation = False 
blosm_props.highways = False  
blosm_props.railways = False   
blosm_props.dataType = "osm"

blosm_props.singleObject = True

blosm_props.gnBlendFile2d = buildifyPath # "C:\\Users\\josif\\Downloads\\buildify_1.0.blend"
blosm_props.gnSetup2d = 'building'

if source_type == "server":
    # Expect 4 additional parameters: min_lat, max_lat, min_lon, max_lon
    if len(script_args) < 5:
        raise ValueError("Server mode requires 4 parameters: min_lat, max_lat, min_lon, max_lon")
    
    min_lat = float(script_args[1])
    max_lat = float(script_args[2])
    min_lon = float(script_args[3])
    max_lon = float(script_args[4])
    
    blosm_props.osmSource = "server"
    # Make sure your addon has properties to store these parameters. For example:
    blosm_props.minLat = min_lat
    blosm_props.maxLat = max_lat
    blosm_props.minLon = min_lon
    blosm_props.maxLon = max_lon
else:
    raise ValueError("Invalid source type. Use 'server'.")

try:
    bpy.ops.blosm.import_data()
    print("BLOSM import succeeded!")
except Exception as e:
    print("Warning: Error during BLOSM import:", e)
    
# bpy.context.object.location[2] = 100

# Select all object and create the mesh 
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.duplicates_make_real()

# Export file to the output path
try:
    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=False,
    )
    print(f"Successfully exported to {output_path}")
except Exception as e:
    print(f"Warning: Error during FBX export: {e}")

print("BLOSM import complete!")
