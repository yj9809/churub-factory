using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Read-only validation after Unity imports the migrated nested prefab overrides.
public static class WorkPointValidation
{
    [MenuItem("Tools/Churub/Validate WorkPoint Bindings")]
    public static void Validate()
    {
        ValidatePrefab("Assets/3. Prefab/Factory Machinery/IngredientSpawn.prefab", 1);
        ValidatePrefab("Assets/3. Prefab/Factory Machinery/ChuruConveyerBelt Obj.prefab", 2);
        ValidatePrefab("Assets/3. Prefab/Factory Machinery/Box Packaging.prefab", 3);

        const string path = "Assets/2. Scene/Game.unity";
        var scene = SceneManager.GetSceneByPath(path);
        bool openedHere = !scene.isLoaded;
        if (openedHere)
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            int count = 0;
            foreach (var root in scene.GetRootGameObjects())
                count += ValidateRoot(root);
            if (count != 15)
                throw new InvalidOperationException($"Expected 15 Game WorkPoints, found {count}.");
        }
        finally
        {
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }
        Debug.Log("WorkPoint bindings validated: 3 machinery prefabs and 15 Game scene WorkPoints.");
    }

    private static void ValidatePrefab(string path, int expected)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            int actual = ValidateRoot(root);
            if (actual != expected)
                throw new InvalidOperationException($"{path}: expected {expected} WorkPoints, found {actual}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static int ValidateRoot(GameObject root)
    {
        var points = root.GetComponentsInChildren<WorkPoint>(true);
        foreach (var point in points)
        {
            var data = new SerializedObject(point);
            var action = data.FindProperty("action").objectReferenceValue as WorkAction;
            if (action == null || action.gameObject != point.gameObject || !action.enabled)
                throw new InvalidOperationException($"{point.name}: missing, misplaced, or disabled WorkAction.");
            var actionData = new SerializedObject(action);
            if (action is ItemTransfer)
            {
                var endpoint = actionData.FindProperty("endpoint").objectReferenceValue as MonoBehaviour;
                if (endpoint == null || !(endpoint is IItemTransferEndpoint))
                    throw new InvalidOperationException($"{point.name}: invalid ItemTransfer endpoint.");
                bool playerOnly = actionData.FindProperty("playerOnly").boolValue;
                bool expected = endpoint is Truck || (endpoint is BoxStorage storage
                    && storage.bsType == BoxStorageType.BoxStorage);
                if (playerOnly != expected)
                    throw new InvalidOperationException($"{point.name}: incorrect player-only permission.");
            }
            else if (action is PackagingInteraction
                && actionData.FindProperty("packaging").objectReferenceValue == null)
            {
                throw new InvalidOperationException($"{point.name}: missing packaging machine.");
            }
        }
        return points.Length;
    }
}
