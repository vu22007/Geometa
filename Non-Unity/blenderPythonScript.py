import bpy
import sys
import os

defaultFilePath = "C:\\Users\\josif\\Downloads\\map.osm"

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
    blosm_props.serverMinLat = min_lat
    blosm_props.serverMaxLat = max_lat
    blosm_props.serverMinLon = min_lon
    blosm_props.serverMaxLon = max_lon

elif source_type == "file":
    blosm_props.osmSource = "file"
    if len(script_args) > 1:
        file_path = script_args[1]
        blosm_props.osmFilepath = file_path
        print(f"Using file mode with path: {file_path}")
    else:
        # Fall back to a default file path if none is provided (or raise an error)
        blosm_props.osmFilepath = defaultFilePath
        print(f"Using file mode with default path: {defaultFilePath}")
else:
    raise ValueError("Invalid source type. Use 'server' or 'file'.")

# Switch to importing from a server
# blosm_props.osmSource = "server"

# Or switch to importing from a local file:
# blosm_props.osmSource = "file"
# blosm_props.osmFilepath = "C:\\Users\\josif\\Downloads\\map.osm"

blosm_props.mode = "2D"
blosm_props.buildings = True   # Import buildings
blosm_props.water = False      # Skip water
blosm_props.forests = False    # Skip forests
blosm_props.vegetation = False # Skip other vegetation
blosm_props.highways = False   # Skip roads and paths
blosm_props.railways = False   # Skip railways
blosm_props.dataType = "osm"

blosm_props.singleObject = True

blosm_props.gnBlendFile2d = "C:\\Users\\josif\\Downloads\\buildify_1.0.blend"
blosm_props.gnSetup2d = 'building'

try:
    bpy.ops.blosm.import_data()
    print("BLOSM import succeeded!")
except Exception as e:
    print("Warning: Error during BLOSM import:", e)
    
bpy.context.object.location[2] = 0

bpy.ops.object.duplicates_make_real()

# output_path = "..\\Assets\\Resources\\Prefabs\\Map\\scriptTest1.fbx"

# Export to FBX - using relative path to Unity project
# Get the directory where the script is running
script_dir = os.path.dirname(os.path.realpath(__file__))
# Construct relative path to Unity Assets/Resources folder
output_path = os.path.normpath(os.path.join(script_dir, "..\\Assets\\Resources\\Prefabs\\Map\\scriptTest1.fbx"))

try:
    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=False,
    )
    print(f"Successfully exported to {output_path}")
except Exception as e:
    print(f"Warning: Error during FBX export: {e}")

print("BLOSM import complete!")
