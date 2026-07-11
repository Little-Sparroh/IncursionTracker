using System;
using BepInEx.Configuration;
using UnityEngine;
using Pigeon.Movement;
using Sparroh.UI;
using TMPro;

public class IncursionTrackerHUD
{
    private ConfigEntry<bool> enableHud;
    private ConfigEntry<bool> stopAtFloor30;
    private ConfigEntry<float> trackerAnchorX;
    private ConfigEntry<float> trackerAnchorY;

    private HudHandle hud;
    private readonly ConfigFile configFile;

    private bool isTracking;
    private bool isFrozen;
    private bool isMissionTimerPaused;
    private float accumulatedMissionTime;
    private float missionSegmentStartTime;
    private float frozenMissionTime;
    private float remainingTime;
    private float frozenRemainingTime;
    private float lastRemainingTimerTickTime;
    private int floor;
    private int abominationsKilled;
    private int leversPulled;
    private int codesInput;

    private const float RemainingTimerPauseGraceSeconds = 1.5f;

    public static IncursionTrackerHUD Instance { get; private set; }

    public IncursionTrackerHUD(ConfigFile configFile)
    {
        this.configFile = configFile;
        Instance = this;

        try
        {
            enableHud = configFile.Bind(
                "General",
                "EnableIncursionTrackerHUD",
                true,
                "Enables the Incursion tracker HUD display.");

            stopAtFloor30 = configFile.Bind(
                "General",
                "StopTrackingAtFloor30",
                false,
                "When enabled, freezes all trackers once the player reaches floor 30.");

            trackerAnchorX = configFile.Bind(
                "HUD Positioning",
                "IncursionTrackerAnchorX",
                0.6512445f,
                "X anchor position for Incursion Tracker (0-1).");
            trackerAnchorY = configFile.Bind(
                "HUD Positioning",
                "IncursionTrackerAnchorY",
                0.93617f,
                "Y anchor position for Incursion Tracker (0-1).");
            trackerAnchorX.SettingChanged += OnAnchorChanged;
            trackerAnchorY.SettingChanged += OnAnchorChanged;

            enableHud.SettingChanged += OnEnableHudChanged;
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Failed to initialize IncursionTrackerHUD: {ex.Message}");
        }
    }

    private bool IsHudAlive => hud != null && hud.GameObject != null && hud.Lines != null && hud.Lines.Length >= 6;

    public bool IsTracking => isTracking;
    public bool IsFrozen => isFrozen;
    public bool IsActive => IsHudAlive && hud.IsActive;

    public void UpdateHudVisibility()
    {
        if (!IsHudAlive)
        {
            ClearDestroyedHud();
            return;
        }

        bool shouldShow = enableHud.Value && isTracking;
        hud.SetActive(shouldShow);
    }

    private void OnEnableHudChanged(object sender, EventArgs e)
    {
        if (!enableHud.Value && hud != null)
            DestroyHud();
        UpdateHudVisibility();
    }

    private void OnAnchorChanged(object sender, EventArgs e)
    {
        if (IsHudAlive)
            hud.SetAnchor(trackerAnchorX.Value, trackerAnchorY.Value);
    }

    private void ClearDestroyedHud()
    {
        if (hud == null) return;
        try
        {
            if (hud.Rect != null)
                HudRepositionClient.Unregister(SparrohPlugin.PluginGUID);
        }
        catch { /* ignore */ }
        hud = null;
    }

    public void StartTracking()
    {
        isTracking = true;
        isFrozen = false;
        isMissionTimerPaused = false;
        accumulatedMissionTime = 0f;
        missionSegmentStartTime = Time.realtimeSinceStartup;
        frozenMissionTime = 0f;
        remainingTime = 0f;
        frozenRemainingTime = 0f;
        lastRemainingTimerTickTime = Time.realtimeSinceStartup;
        floor = 0;
        abominationsKilled = 0;
        leversPulled = 0;
        codesInput = 0;

        if (IncursionObjective.Instance != null)
            floor = IncursionObjective.Instance.CurrentFloor;

        SparrohPlugin.Logger.LogInfo("Incursion tracker started");
        UpdateHudVisibility();
    }

    public void StopTracking()
    {
        isTracking = false;
        isFrozen = false;
        isMissionTimerPaused = false;
        SparrohPlugin.Logger.LogInfo("Incursion tracker stopped");
        UpdateHudVisibility();
    }

    public void SetFloor(int newFloor)
    {
        if (!isTracking || isFrozen) return;
        floor = newFloor;
        TryFreezeAtFloor30();
    }

    public void SetRemainingTime(float time)
    {
        if (!isTracking || isFrozen) return;
        remainingTime = Mathf.Max(0f, time);
        lastRemainingTimerTickTime = Time.realtimeSinceStartup;
        ResumeMissionTimer();
    }

    public void OnAbominationKilled()
    {
        if (!isTracking || isFrozen) return;
        abominationsKilled++;
        SparrohPlugin.Logger.LogInfo($"Abomination killed. Total: {abominationsKilled}");
    }

    public void OnLeverPulled()
    {
        if (!isTracking || isFrozen) return;
        leversPulled++;
        SparrohPlugin.Logger.LogInfo($"Lever pulled. Total: {leversPulled}");
    }

    public void OnCodeInput()
    {
        if (!isTracking || isFrozen) return;
        codesInput++;
        SparrohPlugin.Logger.LogInfo($"Door code input. Total: {codesInput}");
    }

    private void TryFreezeAtFloor30()
    {
        if (!stopAtFloor30.Value || isFrozen) return;
        if (floor < 30) return;

        isFrozen = true;
        PauseMissionTimer();
        frozenMissionTime = GetCurrentMissionTime();
        frozenRemainingTime = remainingTime;
        SparrohPlugin.Logger.LogInfo($"Incursion trackers frozen at floor {floor}");
    }

    private float GetCurrentMissionTime()
    {
        if (isMissionTimerPaused)
            return accumulatedMissionTime;
        return accumulatedMissionTime + (Time.realtimeSinceStartup - missionSegmentStartTime);
    }

    private void PauseMissionTimer()
    {
        if (isMissionTimerPaused) return;
        accumulatedMissionTime += Time.realtimeSinceStartup - missionSegmentStartTime;
        isMissionTimerPaused = true;
    }

    private void ResumeMissionTimer()
    {
        if (!isMissionTimerPaused) return;
        missionSegmentStartTime = Time.realtimeSinceStartup;
        isMissionTimerPaused = false;
    }

    private void UpdateMissionTimerPauseState()
    {
        if (isFrozen) return;
        bool shouldPause = Time.realtimeSinceStartup - lastRemainingTimerTickTime > RemainingTimerPauseGraceSeconds;
        if (shouldPause)
            PauseMissionTimer();
        else
            ResumeMissionTimer();
    }

    private void CreateTrackerHUD()
    {
        if (IsHudAlive) return;
        ClearDestroyedHud();

        hud = HudBuilder.Create("IncursionTrackerHUD")
            .ParentToReticle()
            .Anchor(trackerAnchorX.Value, trackerAnchorY.Value)
            .Pivot(new Vector2(0f, 1f))
            .Size(280f, 140f)
            .AddLines(6, fontSize: 16f, alignment: TextAlignmentOptions.TopLeft)
            .Build();

        if (!IsHudAlive)
            return;

        HudRepositionClient.Register(
            SparrohPlugin.PluginGUID,
            "Incursion Tracker",
            hud.Rect,
            trackerAnchorX,
            trackerAnchorY);

        UpdateHudVisibility();
    }

    private void DestroyHud()
    {
        HudRepositionClient.Unregister(SparrohPlugin.PluginGUID);
        if (hud != null)
        {
            if (hud.GameObject != null)
                hud.Destroy();
            hud = null;
        }
    }

    public void Update()
    {
        try
        {
            if (!isTracking || enableHud == null || !enableHud.Value)
            {
                if (IsHudAlive && hud.IsActive)
                    hud.SetActive(false);
                return;
            }

            if (hud != null && !IsHudAlive)
                ClearDestroyedHud();

            if (!isFrozen && IncursionObjective.Instance != null)
            {
                int current = IncursionObjective.Instance.CurrentFloor;
                if (current != floor)
                    SetFloor(current);
            }

            UpdateMissionTimerPauseState();

            if (Player.LocalPlayer == null || Player.LocalPlayer.PlayerLook == null || Player.LocalPlayer.PlayerLook.Reticle == null)
                return;

            if (!IsHudAlive)
            {
                CreateTrackerHUD();
                return;
            }

            float missionTime = isFrozen ? frozenMissionTime : GetCurrentMissionTime();
            float displayRemaining = isFrozen ? frozenRemainingTime : remainingTime;
            Color color = isFrozen ? UIColors.TextMuted : UIColors.Amber;

            hud.Lines[0].SetRich("Mission", FormatTime(missionTime), color);
            hud.Lines[1].SetRich("Remaining", FormatRemaining(displayRemaining), color);
            hud.Lines[2].SetRich("Floor", floor, color);
            hud.Lines[3].SetRich("Aboms", abominationsKilled, color);
            hud.Lines[4].SetRich("Levers", leversPulled, color);
            hud.Lines[5].SetRich("Codes", codesInput, color);
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in IncursionTrackerHUD.Update(): {ex.Message}");
        }
    }

    private static string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds < 0f) timeInSeconds = 0f;
        int minutes = (int)(timeInSeconds / 60f);
        int seconds = (int)(timeInSeconds % 60f);
        int milliseconds = (int)((timeInSeconds % 1f) * 1000f);
        return $"{minutes:D2}:{seconds:D2}.{milliseconds:D3}";
    }

    private static string FormatRemaining(float timeInSeconds)
    {
        if (timeInSeconds < 0f) timeInSeconds = 0f;
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return $"{minutes}:{seconds:D2}";
    }

    public void OnDestroy()
    {
        try
        {
            DestroyHud();
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogError($"Error in IncursionTrackerHUD.OnDestroy(): {ex.Message}");
        }
    }
}
