using System.Net.NetworkInformation;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Steamworks;

namespace PoPM;

[HarmonyPatch(typeof(FadeController), "FadeOut")]
public class FadeControllerFadeOutPatch
{
    static void Prefix()
    {
        // Do stuff after scene loads
    }
}

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public new static BepInEx.Logging.ManualLogSource Logger = null;
    
    public bool firstSteamworksInit;

    private void OnApplicationQuit()
    {
        LobbySystem.Instance.ExitLobby();
    }

    private void Awake()
    {
        Logger = base.Logger;
        
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        
        new Harmony("patch.popm").PatchAll();
    }

    public static string BuildGUID => Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId.ToString();

    private void OnGUI()
    {
        GUI.Label(new Rect(10, Screen.height - 20, 400, 40), $"PoPM ID: {BuildGUID}");
    }

    void Update()
    {
        if (!SteamManager.Initialized)
            return;

        SteamAPI.RunCallbacks();
        if (!firstSteamworksInit)
        {
            firstSteamworksInit = true;
            
            var lobbyObject = new GameObject();
            lobbyObject.AddComponent<LobbySystem>();
            DontDestroyOnLoad(lobbyObject);

            var netObject = new GameObject();
            netObject.AddComponent<IngameNetManager>();
        }
    }
}

