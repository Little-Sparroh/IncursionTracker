using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency("sparroh.uilibrary")]
[MycoMod(null, ModFlags.IsClientSide)]
public class SparrohPlugin : BaseUnityPlugin

{
    public const string PluginGUID = "sparroh.incursiontracker";
    public const string PluginName = "IncursionTracker";
    public const string PluginVersion = "1.0.2";

    internal static new ManualLogSource Logger;

    private Harmony harmony;
    private IncursionTrackerLoader incursionTracker;

    private void Awake()
    {
        Logger = base.Logger;

        try
        {
            harmony = new Harmony(PluginGUID);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to create Harmony instance: {ex.Message}");
            return;
        }

        var configFile = Config;
        try
        {
            var watcher = new FileSystemWatcher(Paths.ConfigPath, "sparroh.incursiontracker.cfg");
            watcher.Changed += (s, e) =>
            {
                configFile.Reload();
            };
            watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Failed to set up config watcher: {ex.Message}");
        }

        try
        {
            incursionTracker = new IncursionTrackerLoader(configFile, harmony);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to initialize IncursionTracker: {ex.Message}");
        }

        try
        {
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to apply Harmony patches: {ex.Message}");
        }

        Logger.LogInfo($"{PluginName} loaded successfully.");
    }

    private void Update()
    {
        try
        {
            if (incursionTracker != null) incursionTracker.UpdateHudVisibility();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in IncursionTracker.UpdateHudVisibility(): {ex.Message}");
        }

        try
        {
            if (incursionTracker != null) incursionTracker.Update();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in IncursionTracker.Update(): {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        try
        {
            if (incursionTracker != null) incursionTracker.OnDestroy();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in IncursionTracker.OnDestroy(): {ex.Message}");
        }

        try
        {
            if (harmony != null) harmony.UnpatchSelf();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error unpatching Harmony: {ex.Message}");
        }
    }
}
