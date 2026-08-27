using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PortfolioBuild
{
    private const string OutputPath = "Build/Android/Churub-v1.1.0.apk";

    public static void BuildAndroidDevelopment()
    {
        ConfigureAndroidTools();

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputPath = Path.Combine(projectRoot, OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new BuildFailedException("No enabled scenes were found in EditorBuildSettings.");
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException(
                $"Android build failed with result {report.summary.result} and " +
                $"{report.summary.totalErrors} errors.");
        }

        Debug.Log(
            $"Android build succeeded: {outputPath} " +
            $"({report.summary.totalSize} bytes, {report.summary.totalTime}).");
    }

    private static void ConfigureAndroidTools()
    {
        string toolsRoot = Path.Combine(
            EditorApplication.applicationContentsPath,
            "PlaybackEngines",
            "AndroidPlayer");

        AndroidExternalToolsSettings.sdkRootPath = Path.Combine(toolsRoot, "SDK");
        AndroidExternalToolsSettings.ndkRootPath = Path.Combine(toolsRoot, "NDK");
        AndroidExternalToolsSettings.jdkRootPath = Path.Combine(toolsRoot, "OpenJDK");
    }
}
