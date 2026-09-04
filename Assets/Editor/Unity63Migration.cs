using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEngine;

public static class Unity63Migration
{
    private const string ApplicationIdentifier = "com.Churub.ChurubFactory";
    private const string ExpectedVersion = "1.1.0";
    private const string ProfileDirectory = "Assets/Settings/Build Profiles";

    public static void ConfigureProject()
    {
        ValidateIdentityAndVersion();
        ConfigureAndroidPlayerSettings();
        CreateOrUpdateProfile("Android Development", development: true, appBundle: false);
        CreateOrUpdateProfile("Android Release", development: false, appBundle: true);

        AssetDatabase.SaveAssets();
        Debug.Log("Unity 6.3 Android settings and build profiles are configured.");
    }

    private static void ValidateIdentityAndVersion()
    {
        string identifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        if (identifier != ApplicationIdentifier)
        {
            throw new InvalidOperationException(
                $"Application Identifier must remain '{ApplicationIdentifier}', but was '{identifier}'.");
        }

        if (PlayerSettings.bundleVersion != ExpectedVersion)
        {
            throw new InvalidOperationException(
                $"Application version must remain '{ExpectedVersion}', but was " +
                $"'{PlayerSettings.bundleVersion}'.");
        }
    }

    private static void ConfigureAndroidPlayerSettings()
    {
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
        PlayerSettings.SetScriptingBackend(
            NamedBuildTarget.Android,
            ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures =
            AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.Android.minifyDebug = false;
        PlayerSettings.Android.minifyRelease = true;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
    }

    private static void CreateOrUpdateProfile(
        string profileName,
        bool development,
        bool appBundle)
    {
        EnsureDirectory(ProfileDirectory);
        string profilePath = $"{ProfileDirectory}/{profileName}.asset";
        BuildProfile profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(profilePath);

        if (profile == null)
        {
            profile = CreateAndroidBuildProfile();
            profile.name = profileName;
            AssetDatabase.CreateAsset(profile, profilePath);
        }

        profile.overrideGlobalScenes = false;
        SetPlatformProperty(profile, "development", development);
        SetPlatformProperty(profile, "allowDebugging", false);
        SetPlatformProperty(profile, "connectProfiler", false);
        SetPlatformProperty(profile, "buildAppBundle", appBundle);
        EditorUtility.SetDirty(profile);
    }

    private static BuildProfile CreateAndroidBuildProfile()
    {
        MethodInfo createInstance = typeof(BuildProfile).GetMethod(
            "CreateInstance",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(BuildTarget), typeof(StandaloneBuildSubtarget) },
            modifiers: null);

        if (createInstance == null)
        {
            throw new MissingMethodException(
                "Unity 6.3 BuildProfile.CreateInstance API was not found.");
        }

        return (BuildProfile)createInstance.Invoke(
            null,
            new object[] { BuildTarget.Android, StandaloneBuildSubtarget.Player });
    }

    private static void SetPlatformProperty(
        BuildProfile profile,
        string propertyName,
        object value)
    {
        PropertyInfo platformProperty = typeof(BuildProfile).GetProperty(
            "platformBuildProfile",
            BindingFlags.Instance | BindingFlags.NonPublic);
        object platformSettings = platformProperty?.GetValue(profile);

        if (platformSettings == null)
        {
            throw new InvalidOperationException(
                $"Build profile '{profile.name}' has no Android platform settings.");
        }

        PropertyInfo settingProperty = platformSettings.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (settingProperty == null || !settingProperty.CanWrite)
        {
            throw new MissingMemberException(
                platformSettings.GetType().FullName,
                propertyName);
        }

        settingProperty.SetValue(platformSettings, value);
    }

    private static void EnsureDirectory(string assetDirectory)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        Directory.CreateDirectory(Path.Combine(projectRoot, assetDirectory));
    }
}
