using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class RunBlenderScript : MonoBehaviour
{
    // Set the path to your Python script that Blender should run.
    string scriptPath = @"C:\Users\josif\Documents\Geometa\Non-Unity\blenderPythonScript.py";

    void Start()
    {
        // RunBlender();
    }

    void RunBlender(double minLat, double maxLat, double minLon, double maxLon)
    {
        // Set the path to your Blender executable.
        // Make sure to use the correct path for your system.
        string blenderExePath = @"C:\Program Files\Blender Foundation\Blender 4.3\blender.exe";

        // Build the extra arguments for your Blender Python script.
        string extraArgs = "";
        // For server mode, pass "server" followed by the 4 parameters.
        extraArgs = $"server {minLat} {maxLat} {minLon} {maxLon}";

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
