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

        yield return null;
    }
}
