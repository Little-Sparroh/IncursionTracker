using System;
using HarmonyLib;
using Unity.Netcode;


[HarmonyPatch]
public static class IncursionTrackerPatches
{
    private static Action<EnemyBrain> abominationSpawnedHandler;
    private static Action<EnemyBrain> abominationKilledHandler;
    private static Action<RepairableObject> leverRepairedHandler;
    private static RepairableObject subscribedLever;

    // NGO SendTo.Everyone RPCs invoke the method twice on host (Send + Execute).
    // Debounce so only one count is recorded per real code input.
    private static float lastCodeInputRealtime = -999f;
    private const float CodeInputDebounceSeconds = 0.5f;



    private static void EnsureHandlers()
    {
        if (abominationSpawnedHandler == null)
        {
            abominationSpawnedHandler = OnAbominationSpawned;
        }

        if (abominationKilledHandler == null)
        {
            abominationKilledHandler = OnAbominationKilled;
        }

        if (leverRepairedHandler == null)
        {
            leverRepairedHandler = OnLeverRepaired;
        }
    }

    private static bool CanTrack()
    {
        return IncursionTrackerHUD.Instance != null
            && IncursionTrackerHUD.Instance.IsTracking
            && !IncursionTrackerHUD.Instance.IsFrozen
            && IncursionObjective.Instance != null;
    }

    private static void SubscribeAbominationTracking()
    {
        EnsureHandlers();
        EnemyBrain.OnAbominationSpawned -= abominationSpawnedHandler;
        EnemyBrain.OnAbominationSpawned += abominationSpawnedHandler;
    }

    private static void UnsubscribeAbominationTracking()
    {
        if (abominationSpawnedHandler != null)
        {
            EnemyBrain.OnAbominationSpawned -= abominationSpawnedHandler;
        }
        UnsubscribeLever();
    }

    private static void OnAbominationSpawned(EnemyBrain brain)
    {
        if (!CanTrack() || brain == null) return;

        brain.OnKilled -= abominationKilledHandler;
        brain.OnKilled += abominationKilledHandler;
    }

    private static void OnAbominationKilled(EnemyBrain brain)
    {
        if (brain != null)
        {
            brain.OnKilled -= abominationKilledHandler;
        }

        if (!CanTrack()) return;
        IncursionTrackerHUD.Instance.OnAbominationKilled();
    }

    private static void SubscribeLever(RepairableObject lever)
    {
        EnsureHandlers();
        UnsubscribeLever();

        if (lever == null) return;

        subscribedLever = lever;
        subscribedLever.OnRepaired += leverRepairedHandler;
    }

    private static void UnsubscribeLever()
    {
        if (subscribedLever != null && leverRepairedHandler != null)
        {
            subscribedLever.OnRepaired -= leverRepairedHandler;
        }
        subscribedLever = null;
    }

    private static void OnLeverRepaired(RepairableObject lever)
    {
        if (!CanTrack()) return;
        IncursionTrackerHUD.Instance.OnLeverPulled();
    }

    [HarmonyPatch(typeof(IncursionObjective), nameof(IncursionObjective.Setup))]
    [HarmonyPostfix]
    private static void Setup_Postfix(IncursionObjective __instance)
    {
        if (IncursionTrackerHUD.Instance == null) return;

        EnsureHandlers();
        IncursionTrackerHUD.Instance.StartTracking();
        SubscribeAbominationTracking();

        // Lever may already exist if Setup runs after spawn
        try
        {
            var leverField = AccessTools.Field(typeof(IncursionObjective), "addTimeLever");
            if (leverField != null)
            {
                var lever = leverField.GetValue(__instance) as RepairableObject;
                if (lever != null)
                {
                    SubscribeLever(lever);
                }
            }
        }
        catch (Exception ex)
        {
            SparrohPlugin.Logger.LogWarning($"Could not subscribe to existing lever: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(IncursionObjective), nameof(IncursionObjective.OnDestroy))]
    [HarmonyPostfix]
    private static void OnDestroy_Postfix()
    {
        UnsubscribeAbominationTracking();

        if (IncursionTrackerHUD.Instance != null)
        {
            IncursionTrackerHUD.Instance.StopTracking();
        }
    }

    [HarmonyPatch(typeof(IncursionObjective), "SpawnRooms_ClientRpc")]
    [HarmonyPostfix]
    private static void SpawnRooms_ClientRpc_Postfix(IncursionObjective __instance, int currentFloor, int floorsReached)
    {
        if (IncursionTrackerHUD.Instance == null || !IncursionTrackerHUD.Instance.IsTracking) return;
        IncursionTrackerHUD.Instance.SetFloor(currentFloor);
    }

    [HarmonyPatch(typeof(IncursionObjective), "OnCodeInputSuccess_ClientRpc")]
    [HarmonyPostfix]
    private static void OnCodeInputSuccess_ClientRpc_Postfix()
    {
        if (!CanTrack()) return;

        float now = UnityEngine.Time.realtimeSinceStartup;
        if (now - lastCodeInputRealtime < CodeInputDebounceSeconds) return;

        lastCodeInputRealtime = now;
        IncursionTrackerHUD.Instance.OnCodeInput();
    }



    [HarmonyPatch(typeof(IncursionObjective), "OnAddTimeLeverSpawned_ClientRpc")]
    [HarmonyPostfix]
    private static void OnAddTimeLeverSpawned_ClientRpc_Postfix(NetworkBehaviourReference leverRef)
    {
        if (IncursionTrackerHUD.Instance == null) return;

        if (leverRef.TryGet(out RepairableObject lever, (NetworkManager)null))
        {
            SubscribeLever(lever);
        }
    }

    [HarmonyPatch(typeof(IncursionHUD), nameof(IncursionHUD.UpdateTimer))]
    [HarmonyPostfix]
    private static void UpdateTimer_Postfix(float time)
    {
        if (IncursionTrackerHUD.Instance == null || !IncursionTrackerHUD.Instance.IsTracking) return;
        IncursionTrackerHUD.Instance.SetRemainingTime(time);
    }

    [HarmonyPatch(typeof(IncursionHUD), nameof(IncursionHUD.SetFloor))]
    [HarmonyPostfix]
    private static void SetFloor_Postfix(int floor)
    {
        if (IncursionTrackerHUD.Instance == null || !IncursionTrackerHUD.Instance.IsTracking) return;
        IncursionTrackerHUD.Instance.SetFloor(floor);
    }
}
