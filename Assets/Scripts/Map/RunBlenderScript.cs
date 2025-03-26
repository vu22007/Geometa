using Fusion;
using System.Collections;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class RunBlenderScript : NetworkBehaviour
{
    private GameObject mapPrefab;

    // Set the path to your Python script that Blender should run.
    string scriptPath = @"Non-Unity\blenderPythonScript.py";

    // Path to the Blender executable
    public string blenderExePath = @"C:\Program Files\Blender Foundation\Blender 4.3\blender.exe";

    public override void Spawned()
    {
        // RunBlender(51.450, 51.451, -2.603, -2.599);
        // ImportFbxToUnity();
        // SpawnNetworkedMap();
    }

    public IEnumerator RunBlender(double minLat, double maxLat, double minLon, double maxLon)
    {
        // For server mode, pass "server" followed by the 4 parameters.
        string extraArgs = $"-- server {minLat} {maxLat} {minLon} {maxLon}";

        // Blender command line parameters:
        // --background : Run Blender in the background (no GUI)
        // --python : Execute the given Python script.
        string arguments = $"--background --python \"{scriptPath}\" {extraArgs}";

        Debug.Log($"Running Blender with command: {blenderExePath} {arguments}");

        ProcessStartInfo processInfo = new ProcessStartInfo
        {
            FileName = blenderExePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        Process process = new Process { StartInfo = processInfo };

        process.Start();

        // Instead of blocking here, poll every frame or so:
        while (!process.HasExited)
        {
            yield return null; // Let Unity run for a frame
        }

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        Debug.Log("Blender output: " + output);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError("Blender error: " + error);
        }

        ImportFbxToUnity();

        yield return null;
    }

    public void ImportFbxToUnity()
    {
        #if UNITY_EDITOR

        // Refresh the asset database to see the new file
        UnityEditor.AssetDatabase.Refresh();

        // Path to the FBX asset in your project
        string fbxPath = "Assets/Resources/Prefabs/Map/Buildify3DBuildings.fbx";
        // Desired path for the created prefab
        string prefabPath = "Assets/Resources/Prefabs/Map/Buildify3DBuildingsPrefab.prefab";

        // Import the asset - this ensures Unity processes it properly
        GameObject fbxAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        // fbxAsset.AddComponent<NetworkObject>();

        if (fbxAsset != null)
        {
            // Create and save the prefab asset from the FBX asset
            mapPrefab = PrefabUtility.SaveAsPrefabAsset(fbxAsset, prefabPath);
            // mapPrefab.AddComponent<NetworkObject>();

            if (mapPrefab != null)
            {
                Debug.Log("Map asset loaded successfully. Ready to use in scene.");
                //GameObject buildingsInstance = Instantiate(mapPrefab, Vector3.zero, Quaternion.identity);
                //buildingsInstance.transform.eulerAngles = new Vector3(90, 180, 0);
            }
            else
            {
                Debug.LogWarning("Map asset could not be loaded after import.");
            }
        }
    #else
            Debug.LogWarning("Asset import is only available in the Unity Editor.");
    #endif
    }

    //private void SpawnNetworkedMap()
    //{
    //    // Load the map prefab from Resources
    //   //  GameObject mapAsset = Resources.Load<GameObject>("/Prefabs/Map/scriptTest1.prefab");

    //    // Spawn it as a networked object so all clients see it
    //    if (mapPrefab != null)
    //    {
    //        GameObject map = Runner.Spawn(mapPrefab, Vector3.zero, Quaternion.identity).gameObject;
    //        map.transform.eulerAngles = new Vector3(90, 180, 0);
    //        Debug.Log("Map spawned for all players");
    //    }
    //    else
    //    {
    //        Debug.LogError("Failed to load map asset");
    //    }
    //}
}
