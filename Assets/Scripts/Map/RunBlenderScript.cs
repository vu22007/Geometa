using Fusion;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class RunBlenderScript : NetworkBehaviour
{
    private Lobby lobby;
    // Path to Python script that runs blender
    string scriptPath = @"Non-Unity\blenderPythonScript.py";
    // Path to the Blender exe
    public string blenderExePath = @"C:\Program Files\Blender Foundation\Blender 4.3\blender.exe";

    public override void Spawned()
    {
        // The lobby is informed when generation of buildings finishes
        lobby = GetComponentInParent<Lobby>();
    }

    public IEnumerator RunBlender(double minLat, double maxLat, double minLon, double maxLon)
    {
        // For server mode, pass "server" followed by the 4 parameters.
        string extraArgs = $"-- server {minLat} {maxLat} {minLon} {maxLon}";

        // Blender command line parameters:
        // --background : Run Blender in the background (no GUI)
        // --python : Execute the given Python script.
        string arguments = $"--background --python \"{scriptPath}\" {extraArgs} ";

        Debug.Log($"Running Blender with: {blenderExePath} {arguments}");

        ProcessStartInfo processInfo = new ProcessStartInfo
        {
            FileName = blenderExePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = true
        };

        Process process = new Process { StartInfo = processInfo };
        process.Start();

        // Waiting untill process is over
        while (!process.HasExited)
        {
            yield return null; 
        }

        //string output = process.StandardOutput.ReadToEnd();
        //string error = process.StandardError.ReadToEnd();
        //Debug.Log("Blender output: " + output);
        //if (!string.IsNullOrEmpty(error))
        //{
        //    // Print if there is an error
        //    Debug.LogError("Blender error: " + error);
        //}

        //Debug.Log("Ran the python script");
       
        // Wait for the exported file to get created
        bool fileExists = false;
        // I know I am not using a ticker but this is before the game starts :))
        float timeout = 120f; 
        float timer = 0f;

        string buildingsFilepath = Path.Combine(Application.dataPath, "Resources", "Prefabs", "Map", "Buildify3DBuildings.glb");
        Debug.Log("Checking if file " + buildingsFilepath + " exists");

        while (!fileExists && timer < timeout)
        {
            fileExists = File.Exists(buildingsFilepath);
            if (!fileExists)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }
        // If timer expired and file still doesn't exist don't rpc call
        if (!fileExists)
        {
            Debug.LogError("Exported file not found!");
            yield break;
        }

        GameObject buildingsPrefab = Resources.Load<GameObject>("Prefabs/Map/Buildify3DBuildings");
        GameObject buildingsInstance = Instantiate(buildingsPrefab, Vector3.zero, Quaternion.identity);
        // The building has to be rotated to match the 2D map
        buildingsInstance.transform.eulerAngles = new Vector3(90, 180, 0);

        // Notify generation of map is ende
        lobby.RPC_MapGenComplete(Runner.LocalPlayer);

        yield return null;
    }
}
