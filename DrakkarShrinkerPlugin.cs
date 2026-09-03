using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace DrakkarShrinker;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class DrakkarShrinkerPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "badbatch.drakkarshrinker";
    public const string PluginName = "Drakkar Shrinker";
    public const string PluginVersion = "1.0.0";

    internal static ConfigEntry<float> Scale = null!;
    internal static ManualLogSource Log = null!;

    private void Awake()
    {
        Scale = Config.Bind(
            "General",
            "Scale",
            0.65f,
            new ConfigDescription(
                "Uniform scale for the Drakkar. Restart Valheim after changing this value.",
                new AcceptableValueRange<float>(0.35f, 1.0f)));

        Log = Logger;
        new Harmony(PluginGuid).PatchAll();
    }
}

/// <summary>
/// Changes the vanilla prefab before the game creates any Drakkar instances.
/// Scaling the root applies consistently to its model, colliders, storage, and ship components.
/// </summary>
[HarmonyPatch(typeof(ZNetScene), "Awake")]
internal static class ZNetSceneAwakePatch
{
    private const string DrakkarPrefabName = "VikingShip_Ashlands";
    private static readonly HashSet<int> RescaledPrefabs = new();

    private static void Postfix(ZNetScene __instance)
    {
        GameObject? drakkar = __instance.GetPrefab(DrakkarPrefabName);
        if (drakkar == null)
        {
            DrakkarShrinkerPlugin.Log.LogWarning($"Could not find the {DrakkarPrefabName} prefab; no Drakkar was resized.");
            return;
        }

        int prefabId = drakkar.GetInstanceID();
        if (!RescaledPrefabs.Add(prefabId))
            return;

        float scale = DrakkarShrinkerPlugin.Scale.Value;
        drakkar.transform.localScale *= scale;
        DrakkarShrinkerPlugin.Log.LogInfo($"Resized Drakkar to {scale:P0} of its normal size.");
    }
}
