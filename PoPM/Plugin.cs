using System.Reflection;
using BepInEx;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace PoPM
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public new static BepInEx.Logging.ManualLogSource Logger = null;

        public bool firstSteamworksInit;

        public static string BuildGUID => Assembly.GetExecutingAssembly().ManifestModule.ModuleVersionId.ToString();

        private void Awake()
        {
            Logger = base.Logger;

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            new Harmony("patch.popm").PatchAll();
        }

        void Update()
        {
            if (!SteamManager.Initialized)
                return;

            SteamAPI.RunCallbacks();
            if (!firstSteamworksInit)
            {
                firstSteamworksInit = true;

                StartCoroutine(NameTag.LoadAssetBundle(Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("PoPM.assets.nametag")));

                var lobbyObject = new GameObject();
                lobbyObject.AddComponent<LobbySystem>();
                DontDestroyOnLoad(lobbyObject);

                var netObject = new GameObject();
                netObject.AddComponent<IngameNetManager>();
                DontDestroyOnLoad(netObject);
            }
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, Screen.height - 20, 400, 40), $"PoPM ID: {BuildGUID}");
        }

        private void OnApplicationQuit()
        {
            IngameNetManager.ExitGame();
            LobbySystem.Instance.ExitLobby();
        }
    }
}