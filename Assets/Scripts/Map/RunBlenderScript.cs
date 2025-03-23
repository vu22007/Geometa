using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class RunBlenderScript : MonoBehaviour
{
    // Set the path to your Python script that Blender should run.
    string scriptPath = @"Non-Unity\blenderPythonScript.py";

    // Path to the Blender executable
    public string blenderExePath = @"C:\Program Files\Blender Foundation\Blender 4.3\blender.exe";

    // Path to load the FBX into Unity after export
    public string unityImportPath = "Assets/ImportedMaps/";

    void Start()
    {
        RunBlender(51.450, -2.603, 51.451, -2.599);
        ImportFbxToUnity();
    }

    void RunBlender(double minLat, double maxLat, double minLon, double maxLon)
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
        process.WaitForExit();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        Debug.Log("Blender output: " + output);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError("Blender error: " + error);
        }
    }

    private void ImportFbxToUnity()
    {
        #if UNITY_EDITOR

        // Copy the FBX file to the Unity project if it's not already there
        string fileName = "scriptTest1.fbx";

        try
        {
            // Refresh the asset database to see the new file
            UnityEditor.AssetDatabase.Refresh();

            // Get the relative path for Unity's asset database
            string relativePath = "Assets/Resources/Prefabs/Map/" + fileName;

            // Import the asset - this ensures Unity processes it properly
            UnityEditor.AssetDatabase.ImportAsset(relativePath, UnityEditor.ImportAssetOptions.ForceUpdate);

            // Optionally, load the asset and use it (for example, instantiate it in the scene)
            GameObject mapPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
            if (mapPrefab != null)
            {
                Debug.Log("Map asset loaded successfully. Ready to use in scene.");

                // Optionally instantiate the map in the scene
                // Instantiate(mapPrefab, Vector3.zero, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning("Map asset could not be loaded after import.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error importing FBX file: {e.Message}");
        }
#else
        Debug.LogWarning("Asset import is only available in the Unity Editor.");
#endif
    }

    void RunBlender(string filePath)
    {
        // Set the path to your Blender executable.
        // Make sure to use the correct path for your system.
        string blenderExePath = @"C:\Program Files\Blender Foundation\Blender 4.3\blender.exe";

        // Build the extra arguments for your Blender Python script.
        string extraArgs = "";
        // For file mode, pass "file" and the file path.
        extraArgs = $"file \"{filePath}\"";

        // Blender command line parameters:
        // --background : Run Blender in the background (no GUI)
        // --python : Execute the given Python script.
        string arguments = $"--background --python \"{scriptPath}\"";

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
        process.WaitForExit();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        Debug.Log("Blender output: " + output);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError("Blender error: " + error);
        }
    }
}
