using System.IO;
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PortfolioBuild
{
    private const string ApplicationIdentifier = "com.Churub.ChurubFactory";
    private const string ExpectedVersion = "1.1.0";
    private const string DevelopmentProfilePath =
        "Assets/Settings/Build Profiles/Android Development.asset";
    private const string ReleaseProfilePath =
        "Assets/Settings/Build Profiles/Android Release.asset";

    public static void BuildAndroidDevelopment()
    {
        ConfigureSharedAndroidSettings();
        Build(
            DevelopmentProfilePath,
            GetOutputPath("development", "apk"));
    }

    public static void BuildAndroidReleaseValidation()
    {
        ConfigureSharedAndroidSettings();

        if (!PlayerSettings.Android.useCustomKeystore)
        {
            UnityEngine.Debug.LogWarning(
                "Building a validation-only AAB without the production keystore. " +
                "This artifact must not be uploaded to Google Play.");
        }

        Build(
            ReleaseProfilePath,
            GetOutputPath("release-validation", "aab"));
    }

    public static void BuildAndroidRelease()
    {
        ConfigureSharedAndroidSettings();
        ConfigureReleaseSigningFromEnvironment();
        Build(
            ReleaseProfilePath,
            GetOutputPath("release", "aab"));
    }

    private static void Build(string profilePath, string outputPath)
    {
        BuildProfile profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(profilePath);
        if (profile == null)
        {
            throw new BuildFailedException(
                $"Build profile was not found at '{profilePath}'. " +
                "Run Unity63Migration.ConfigureProject first.");
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string absoluteOutputPath = Path.Combine(projectRoot, outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absoluteOutputPath));

        if (profile.GetScenesForBuild().Length == 0)
        {
            throw new BuildFailedException(
                $"Build profile '{profile.name}' has no enabled scenes.");
        }

        var options = new BuildPlayerWithProfileOptions
        {
            buildProfile = profile,
            locationPathName = absoluteOutputPath,
            options = BuildOptions.CleanBuildCache | BuildOptions.DetailedBuildReport
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException(
                $"Android build failed with result {report.summary.result} and " +
                $"{report.summary.totalErrors} errors.");
        }

        UnityEngine.Debug.Log(
            $"Android build succeeded: {absoluteOutputPath} " +
            $"({report.summary.totalSize} bytes, {report.summary.totalTime}).");
    }

    private static void ConfigureSharedAndroidSettings()
    {
        if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) !=
            ApplicationIdentifier)
        {
            throw new BuildFailedException(
                "Application Identifier changed unexpectedly. " +
                $"Expected '{ApplicationIdentifier}'.");
        }

        if (PlayerSettings.bundleVersion != ExpectedVersion)
        {
            throw new BuildFailedException(
                "Application version changed unexpectedly. " +
                $"Expected '{ExpectedVersion}'.");
        }

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
        PlayerSettings.SetScriptingBackend(
            NamedBuildTarget.Android,
            ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures =
            AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.Android.minifyDebug = false;
        PlayerSettings.Android.minifyRelease = true;
    }

    private static void ConfigureReleaseSigningFromEnvironment()
    {
        string keystorePath = GetRequiredEnvironmentVariable("CHURUB_KEYSTORE_PATH");
        string keystorePassword = GetRequiredEnvironmentVariable("CHURUB_KEYSTORE_PASS");
        string keyAliasName = GetRequiredEnvironmentVariable("CHURUB_KEYALIAS_NAME");
        string keyAliasPassword = GetRequiredEnvironmentVariable("CHURUB_KEYALIAS_PASS");

        if (!File.Exists(keystorePath))
        {
            throw new BuildFailedException(
                $"Release keystore does not exist: '{keystorePath}'.");
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = Path.GetFullPath(keystorePath);
        PlayerSettings.Android.keystorePass = keystorePassword;
        PlayerSettings.Android.keyaliasName = keyAliasName;
        PlayerSettings.Android.keyaliasPass = keyAliasPassword;
    }

    private static string GetRequiredEnvironmentVariable(string name)
    {
        string value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BuildFailedException(
                $"Required release signing environment variable '{name}' is not set.");
        }

        return value;
    }

    private static string GetOutputPath(string flavor, string extension)
    {
        return Path.Combine(
            "Build",
            "Android",
            $"Churub-v{PlayerSettings.bundleVersion}-{flavor}.{extension}");
    }
}
