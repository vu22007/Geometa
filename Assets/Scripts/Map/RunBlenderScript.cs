using Fusion;
using System.Collections;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class RunBlenderScript : NetworkBehaviour
{
    private GameObject mapPrefab;

    private Lobby lobby;

    // Set the path to your Python script that Blender should run.
    string scriptPath = @"Non-Unity\blenderPythonScript.py";

    // Path to the Blender executable
    public string blenderExePath = @"C:\Program Files\Blender Foundation\Blender 4.3\blender.exe";

    public override void Spawned()
    {
        lobby = GetComponentInParent<Lobby>();
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

        GameObject buildingsPrefab = Resources.Load<GameObject>("Prefabs/Map/Buildify3DBuildings");

        GameObject buildingsInstance = Instantiate(buildingsPrefab, Vector3.zero, Quaternion.identity);
        // The building has to be rotated to match the 2D map
        buildingsInstance.transform.eulerAngles = new Vector3(90, 180, 0);

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
            // Create prefab 
            mapPrefab = PrefabUtility.SaveAsPrefabAsset(fbxAsset, prefabPath);

            if (mapPrefab != null)
            {
                Debug.Log("Map asset loaded successfully. Ready to use in scene.");
                lobby.RPC_MapGenComplete();
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
}
