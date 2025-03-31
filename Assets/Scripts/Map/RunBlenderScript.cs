using Fusion;
using GLTFast;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class RunBlenderScript : NetworkBehaviour
{
    private string outputFilePath; 
    private Lobby lobby;
    string scriptPath; 
    public string blenderExePath; 

    public override void Spawned()
    {
        // The lobby is informed when generation of buildings finishes
        lobby = GetComponentInParent<Lobby>();
        // Path where the glTfF will be outputted
        outputFilePath = Path.Combine(Application.persistentDataPath, "Buildify3DBuildings.glb");
        // outputFilePath = Path.Combine("/", "home", "qc22435", "Documents", "Buildify3DBuildings.glb");
        // Path to Python script that runs blender
        scriptPath = Path.Combine(Application.streamingAssetsPath, "Non-Unity", "blenderPythonScript.py");

        Debug.Log("Data path: " + Application.dataPath);
        Debug.Log("Persistent pat: " + Application.persistentDataPath);
        Debug.Log("Streaming assets: " + Application.streamingAssetsPath);
        
        // Path to the Blender exe - DEPENDENT ON OS
        // blenderExePath = Path.Combine("/", "opt", "blender", "4.1.1", "blender-uob-launcher");
        Debug.Log(blenderExePath);
        blenderExePath =  Path.Combine("C:\\", "Program Files", "Blender Foundation", "Blender 4.3", "blender.exe");
    }

    public IEnumerator RunBlender(double minLat, double maxLat, double minLon, double maxLon)
    {
        // For server mode, pass "server" followed by the 4 parameters.
        string extraArgs = $"-- server {minLat} {maxLat} {minLon} {maxLon} {outputFilePath}";

        // Blender command line parameters:
        // --background: Run Blender in the background (no GUI)
        // --python: Execute the given Python script.
        string arguments = $"--background --python \"{scriptPath}\" {extraArgs}";

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

        // Notify generation of map is ended
        lobby.RPC_MapGenComplete(Runner.LocalPlayer);

        yield return null;
    }
}
