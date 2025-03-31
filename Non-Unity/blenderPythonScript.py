import bpy
import sys
import os
import tempfile

# Get the directory where the script is running
script_dir = os.path.dirname(os.path.realpath(__file__))
# Filepath where the file will be exported
# output_path = "C:\\Users\\josif\\AppData\\LocalLow\\DefaultCompany\\Geometa\\Buildify3DBuildings" # os.path.normpath(os.path.join(script_dir, "..\\Assets\\Resources\\Prefabs\\Map\\Buildify3DBuildings"))
# Path to buildify file 
buildifyPath = os.path.normpath(os.path.join(script_dir, "buildify_1.0.blend"))
# Path to BLOSM 
blosm_addon_path = os.path.normpath(os.path.join(script_dir, "blosm_2.7.13.zip"))

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
    idx= argv.index("--")
    script_args= argv[idx+1:]
else:
    script_args= []

# Default to file mode if no arguments are provided
if not script_args:
    print("No arguments provided, using default file mode")
    source_type = "file"
else:
    source_type = script_args[0].lower()
    print(f"Source type: {source_type}")
    print(f"All arguments: {script_args}")

blosm_props = bpy.context.scene.blosm
osm_files_dir = tempfile.mkdtemp()
bpy.context.preferences.addons["blosm"].preferences.dataDir = osm_files_dir
blosm_props.mode = "2D"
blosm_props.buildings = True   # Import just buildings
blosm_props.water = False      
blosm_props.forests = False    
blosm_props.vegetation =False 
blosm_props.highways = False  
blosm_props.railways= False   
blosm_props.dataType = "osm"

blosm_props.singleObject = True

blosm_props.gnBlendFile2d = buildifyPath # "C:\\Users\\josif\\Downloads\\buildify_1.0.blend"
blosm_props.gnSetup2d = 'building'

if source_type == "server":
    # Expect 4 additional parameters: min_lat, max_lat, min_lon, max_lon
    if len(script_args) < 5:
        raise ValueError("Server mode requires 4 parameters: min_lat, max_lat, min_lon, max_lon")
    
    min_lat =float(script_args[1])
    max_lat = float(script_args[2])
    min_lon =float(script_args[3])
    max_lon = float(script_args[4])
    output_path = script_args[5]
    print(output_path)

    blosm_props.osmSource = "server"
    # Make sure your addon has properties to store these parameters. For example:
    blosm_props.minLat=min_lat
    blosm_props.maxLat=max_lat
    blosm_props.minLon=min_lon
    blosm_props.maxLon=max_lon
else:
    raise ValueError("Invalid source type. Use 'server'.")

try:
    bpy.ops.blosm.import_data()
    print("BLOSM import succeeded!")
except Exception as e:
    print("Error during BLOSM import: ", e)
    
# bpy.context.object.location[2] = 100

# for area in bpy.context.screen.areas:
#     if area.type == 'PROPERTIES':
#         bpy.context.area = area
#         break

obj_name = "map.osm_buildings"
obj = bpy.data.objects.get(obj_name)

# Select and activate the object
bpy.ops.object.select_all(action='DESELECT')
obj.select_set(True)
bpy.context.view_layer.objects.active = obj

collection_name = "map.osm"

# Link object to the collection
target_collection = bpy.data.collections[collection_name]
if obj.name not in target_collection.objects:
    target_collection.objects.link(obj)

obj.hide_set(False)  # Make visible in the viewport
obj.hide_render = False  # Include in renders/exports

# Select all objects and create the mesh 
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.duplicates_make_real()

# Export file to the output path
try:
    bpy.ops.export_scene.gltf(
        filepath=output_path,
        use_selection=False,
        export_apply=True,
        # export_gn_mesh = True
    )
    print(f"Successfully exported to {output_path}")
except Exception as e:
    print(f"Error during glTF export: {e}")

print("BLOSM import complete!")
