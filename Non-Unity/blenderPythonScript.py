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
    source_type = "file"
else:
    source_type = script_args[0].lower()

if source_type == "server":
    # Expect 4 additional parameters: min_lat, max_lat, min_lon, max_lon
    if len(script_args) < 5:
        raise ValueError("Server mode requires 4 parameters: min_lat, max_lat, min_lon, max_lon")
    
    min_lat = script_args[1]
    min_lon = script_args[2]
    max_lat = script_args[3]
    max_lon = script_args[4]
    
    bpy.context.scene.blosm.osmSource = "server"
    # Make sure your addon has properties to store these parameters. For example:
    bpy.context.scene.blosm.serverMinLat = min_lat
    bpy.context.scene.blosm.serverMaxLat = max_lat
    bpy.context.scene.blosm.serverMinLon = min_lon
    bpy.context.scene.blosm.serverMaxLon = max_lon

elif source_type == "file":
    bpy.context.scene.blosm.osmSource = "file"
    if len(script_args) > 1:
        file_path = script_args[1]
        bpy.context.scene.blosm.osmFilepath = file_path
    else:
        # Fall back to a default file path if none is provided (or raise an error)
        bpy.context.scene.blosm.osmFilepath = defaultFilePath
else:
    raise ValueError("Invalid source type. Use 'server' or 'file'.")

# Switch to importing from a server
# bpy.context.scene.blosm.osmSource = "server"

# Or switch to importing from a local file:
# bpy.context.scene.blosm.osmSource = "file"
# bpy.context.scene.blosm.osmFilepath = "C:\\Users\\josif\\Downloads\\map.osm"

bpy.context.scene.blosm.mode = "2D"

blosm_props = bpy.context.scene.blosm
blosm_props.buildings = True   # Import buildings
blosm_props.water = False      # Skip water
blosm_props.forests = False    # Skip forests
blosm_props.vegetation = False # Skip other vegetation
blosm_props.highways = False   # Skip roads and paths
blosm_props.railways = False   # Skip railways
blosm_props.dataType = "osm"

bpy.context.scene.blosm.singleObject = True

bpy.context.scene.blosm.gnBlendFile2d = "C:\\Users\\josif\\Downloads\\buildify_1.0.blend"
bpy.context.scene.blosm.gnSetup2d = 'building'

try:
    bpy.ops.blosm.import_data()
    print("BLOSM import succeeded!")
except Exception as e:
    print("Warning: Error during BLOSM import:", e)
    
bpy.context.object.location[2] = 0

bpy.ops.object.duplicates_make_real()

bpy.ops.export_scene.fbx(
    filepath="C:\\Users\\josif\\Downloads\\scriptTest1.fbx",
    use_selection=False,
)

print("BLOSM import complete!")
