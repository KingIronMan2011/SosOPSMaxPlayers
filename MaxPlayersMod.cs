using MelonLoader;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSOSOPS.Constants;
using Il2CppSOSOPS.Game.Network;
using Il2CppSOSOPS.Game.Network.States;
using Il2CppSOSOPS.Game.Network.NetworkSession.Modules;
using Il2CppSOSOPS.Game.Environment.DynamicMenu;
using Il2CppSOSOPS.UI.MainMenu.Tabs;
using Il2CppSOSOPS.UI.PlayerList;
using Il2CppSOSOPS.UI.Lobby;
using Il2CppSOSOPS.UI.Gameplay.Leaderboard;
using Il2CppSOSOPS.Utility;
using Il2CppSteamworks;
using UnityEngine;

[assembly: MelonInfo(typeof(SOSOpsMaxPlayers.MaxPlayersMod), "SOS Ops Max Players Mod", "1.3.0", "KingIronMan2011")]
[assembly: MelonGame("ArtDock", "SOS OPS")]

namespace SOSOpsMaxPlayers
{
    public class MaxPlayersMod : MelonMod
    {
        public static MelonPreferences_Category ConfigCategory;
        public static MelonPreferences_Entry<int> MaxPlayersEntry;

        public static int TargetMaxPlayers => MaxPlayersEntry != null ? MaxPlayersEntry.Value : 8;

        public override void OnInitializeMelon()
        {
            ConfigCategory = MelonPreferences.CreateCategory("SOSOpsMaxPlayers");
            MaxPlayersEntry = ConfigCategory.CreateEntry("MaxPlayers", 8, "Maximum Player Limit", "Sets the maximum number of players allowed in a lobby.");

            LoggerInstance.Msg($"SOS Ops Max Players Mod Loaded! Configured limit: {TargetMaxPlayers}");

            try
            {
                NetworkConstants.MAX_PLAYERS = TargetMaxPlayers;
                LoggerInstance.Msg($"Successfully set NetworkConstants.MAX_PLAYERS to {TargetMaxPlayers}");
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Warning($"Could not set NetworkConstants.MAX_PLAYERS directly: {ex.Message}");
            }
        }
    }

    // ==========================================
    // ANTI-MOD DETECTOR BYPASS (Prevents Game Crash/Termination)
    // ==========================================

    [HarmonyPatch(typeof(ModDetector), nameof(ModDetector.Awake))]
    public static class Patch_ModDetector_Awake
    {
        public static bool Prefix()
        {
            Melon<MaxPlayersMod>.Logger.Msg("[Security] Neutered ModDetector.Awake - preventing mod detection crash.");
            return false;
        }
    }

    [HarmonyPatch(typeof(ModDetector), nameof(ModDetector.CheckAssemblies))]
    public static class Patch_ModDetector_CheckAssemblies
    {
        public static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(ModDetector), nameof(ModDetector.CheckProcessModules))]
    public static class Patch_ModDetector_CheckProcessModules
    {
        public static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(ModDetector), nameof(ModDetector.CheckFileSystem))]
    public static class Patch_ModDetector_CheckFileSystem
    {
        public static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(ModDetector), nameof(ModDetector.CrashGame))]
    public static class Patch_ModDetector_CrashGame
    {
        public static bool Prefix()
        {
            Melon<MaxPlayersMod>.Logger.Msg("[Security] Intercepted and blocked ModDetector.CrashGame()!");
            return false;
        }
    }

    [HarmonyPatch(typeof(ModDetector), nameof(ModDetector.TerminateProcess))]
    public static class Patch_ModDetector_TerminateProcess
    {
        public static bool Prefix(ref bool __result)
        {
            Melon<MaxPlayersMod>.Logger.Msg("[Security] Intercepted and blocked ModDetector.TerminateProcess()!");
            __result = true;
            return false;
        }
    }

    // ==========================================
    // NETWORKING & LOBBY PATCHES
    // ==========================================

    [HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.CreateLobbyAsync))]
    public static class Patch_SteamMatchmaking_CreateLobbyAsync
    {
        public static void Prefix(ref int maxMembers)
        {
            Melon<MaxPlayersMod>.Logger.Msg($"[SteamMatchmaking] Overriding CreateLobbyAsync maxMembers from {maxMembers} to {MaxPlayersMod.TargetMaxPlayers}");
            maxMembers = MaxPlayersMod.TargetMaxPlayers;
        }
    }

    [HarmonyPatch(typeof(HostingState), nameof(HostingState.GetConnectingStatus))]
    public static class Patch_HostingState_GetConnectingStatus
    {
        public static void Postfix(HostingState __instance, ref NetworkStatus __result)
        {
            if (__result == NetworkStatus.ServerIsFull)
            {
                var sessionService = __instance._sessionService;
                if (sessionService != null && sessionService.Instance != null)
                {
                    int currentCount = sessionService.Instance.PlayerCount;
                    if (currentCount < MaxPlayersMod.TargetMaxPlayers)
                    {
                        Melon<MaxPlayersMod>.Logger.Msg($"[HostingState] Overriding ServerIsFull: Current players {currentCount} < Max {MaxPlayersMod.TargetMaxPlayers}. Approving connection.");
                        __result = NetworkStatus.Success;
                    }
                }
                else
                {
                    Melon<MaxPlayersMod>.Logger.Msg("[HostingState] Overriding ServerIsFull to Success (session service null).");
                    __result = NetworkStatus.Success;
                }
            }
        }
    }

    [HarmonyPatch(typeof(TextChatSessionModule), nameof(TextChatSessionModule.GetColorByIndex))]
    public static class Patch_TextChat_ColorSafety
    {
        public static void Prefix(ref sbyte index)
        {
            if (index < 0) index = 0;
            // Wrap index around 4 (standard color palette size) to prevent IndexOutOfRangeException
            index = (sbyte)(index % 4);
        }
    }

    // ==========================================
    // DYNAMIC MENU 3D CHARACTER SAFETY
    // ==========================================

    [HarmonyPatch(typeof(DynamicMenuExperience), nameof(DynamicMenuExperience.LobbyPlayerJoined))]
    public static class Patch_DynamicMenuExperience_LobbyPlayerJoined
    {
        public static bool Prefix(DynamicMenuExperience __instance, int index)
        {
            if (index < 0 || index >= 4)
            {
                return false;
            }
            try
            {
                if (__instance.spawnedLobbyPlayers == null || index >= __instance.spawnedLobbyPlayers.Count)
                    return false;
            }
            catch
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(DynamicMenuExperience), nameof(DynamicMenuExperience.LobbyPlayerLeaved))]
    public static class Patch_DynamicMenuExperience_LobbyPlayerLeaved
    {
        public static bool Prefix(DynamicMenuExperience __instance, int index)
        {
            if (index < 0 || index >= 4)
            {
                return false;
            }
            try
            {
                if (__instance.spawnedLobbyPlayers == null || index >= __instance.spawnedLobbyPlayers.Count)
                    return false;
            }
            catch
            {
                return false;
            }
            return true;
        }
    }

    // ==========================================
    // UI EXTENSIONS FOR EXTRA PLAYER SLOTS
    // ==========================================

    [HarmonyPatch(typeof(UILobbyMenuTab), nameof(UILobbyMenuTab.OnMembersUpdated))]
    public static class Patch_UILobbyMenuTab_OnMembersUpdated
    {
        public static void Prefix(UILobbyMenuTab __instance)
        {
            try
            {
                var elements = __instance._playerElements;
                if (elements == null || elements.Count == 0 || elements.Count >= MaxPlayersMod.TargetMaxPlayers)
                    return;

                int originalCount = elements.Count;
                int targetCount = MaxPlayersMod.TargetMaxPlayers;
                var template = elements[originalCount - 1];
                if (template == null || template.gameObject == null) return;

                var parent = template.transform.parent;
                var newArray = new Il2CppReferenceArray<UILobbyPlayerElement>(targetCount);
                for (int i = 0; i < originalCount; i++)
                {
                    newArray[i] = elements[i];
                }

                for (int i = originalCount; i < targetCount; i++)
                {
                    var newGo = UnityEngine.Object.Instantiate(template.gameObject, parent);
                    newGo.name = $"PlayerElement_{i}";
                    var comp = newGo.GetComponent<UILobbyPlayerElement>();
                    if (comp != null)
                    {
                        comp.Reset();
                        newArray[i] = comp;
                    }
                }

                __instance._playerElements = newArray;
                Melon<MaxPlayersMod>.Logger.Msg($"[UI] Successfully expanded Main Menu Lobby cards from {originalCount} to {targetCount}.");
            }
            catch (System.Exception ex)
            {
                Melon<MaxPlayersMod>.Logger.Warning($"[UI] Could not expand UILobbyMenuTab slots: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(UIPlayerList), nameof(UIPlayerList.OnMembersUpdated))]
    public static class Patch_UIPlayerList_OnMembersUpdated
    {
        public static void Prefix(UIPlayerList __instance)
        {
            try
            {
                var elements = __instance._playerElements;
                if (elements == null || elements.Count == 0 || elements.Count >= MaxPlayersMod.TargetMaxPlayers)
                    return;

                int originalCount = elements.Count;
                int targetCount = MaxPlayersMod.TargetMaxPlayers;
                var template = elements[originalCount - 1];
                if (template == null || template.gameObject == null) return;

                var parent = template.transform.parent;
                var newArray = new Il2CppReferenceArray<UIPlayerListElement>(targetCount);
                for (int i = 0; i < originalCount; i++)
                {
                    newArray[i] = elements[i];
                }

                for (int i = originalCount; i < targetCount; i++)
                {
                    var newGo = UnityEngine.Object.Instantiate(template.gameObject, parent);
                    newGo.name = $"PlayerListElement_{i}";
                    var comp = newGo.GetComponent<UIPlayerListElement>();
                    if (comp != null)
                    {
                        comp.Reset();
                        newArray[i] = comp;
                    }
                }

                __instance._playerElements = newArray;
                Melon<MaxPlayersMod>.Logger.Msg($"[UI] Successfully expanded Pause Menu player list from {originalCount} to {targetCount}.");
            }
            catch (System.Exception ex)
            {
                Melon<MaxPlayersMod>.Logger.Warning($"[UI] Could not expand UIPlayerList slots: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(UILeaderboardOverlay), nameof(UILeaderboardOverlay.OnMembersUpdated))]
    public static class Patch_UILeaderboardOverlay_OnMembersUpdated
    {
        public static void Prefix(UILeaderboardOverlay __instance)
        {
            try
            {
                var elements = __instance._elements;
                if (elements == null || elements.Count == 0 || elements.Count >= MaxPlayersMod.TargetMaxPlayers)
                    return;

                int originalCount = elements.Count;
                int targetCount = MaxPlayersMod.TargetMaxPlayers;
                var template = elements[originalCount - 1];
                if (template == null || template.gameObject == null) return;

                var parent = template.transform.parent;
                var newArray = new Il2CppReferenceArray<UILeaderboardElement>(targetCount);
                for (int i = 0; i < originalCount; i++)
                {
                    newArray[i] = elements[i];
                }

                for (int i = originalCount; i < targetCount; i++)
                {
                    var newGo = UnityEngine.Object.Instantiate(template.gameObject, parent);
                    newGo.name = $"LeaderboardElement_{i}";
                    var comp = newGo.GetComponent<UILeaderboardElement>();
                    if (comp != null)
                    {
                        newArray[i] = comp;
                    }
                }

                __instance._elements = newArray;
                Melon<MaxPlayersMod>.Logger.Msg($"[UI] Successfully expanded Tab Leaderboard from {originalCount} to {targetCount}.");
            }
            catch (System.Exception ex)
            {
                Melon<MaxPlayersMod>.Logger.Warning($"[UI] Could not expand UILeaderboardOverlay slots: {ex.Message}");
            }
        }
    }
}
