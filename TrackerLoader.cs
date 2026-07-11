using BepInEx.Configuration;
using HarmonyLib;

public class IncursionTrackerLoader
{
    private IncursionTrackerHUD trackerHUD;
    private readonly ConfigFile configFile;
    private readonly Harmony harmony;

    public IncursionTrackerLoader(ConfigFile configFile, Harmony harmony)
    {
        this.configFile = configFile;
        this.harmony = harmony;

        trackerHUD = new IncursionTrackerHUD(configFile);

    }

    public void UpdateHudVisibility()
    {
        trackerHUD.UpdateHudVisibility();
    }

    public void Update()
    {
        trackerHUD.Update();
    }

    public void OnDestroy()
    {
        trackerHUD.OnDestroy();
    }
}
