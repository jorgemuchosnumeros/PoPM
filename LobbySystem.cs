using System;
using System.Collections.Generic;
using HarmonyLib;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PoPM
{
    // Called every time in menu
    [HarmonyPatch(typeof(MainMenu), "Initialize")]
    public class MainMenuInitializePatch
    {
        static void Prefix()
        {
            LobbySystem.instance.MainMenu = GameObject.Find("Menu/Canvas/MainMenu/VerticalGroup/");

            if (!LobbySystem.instance.isPauseMenu)
            {
                LobbySystem.instance.isInGame = false;
                LobbySystem.instance.isGameLoaded = true;
            }
        }
    }

    // Called every time in menu except after splash
    [HarmonyPatch(typeof(MainMenu), "ChangeToPauseMenu")]
    public class MainMenuChangeToPauseMenuPatch
    {
        static void Prefix()
        {
            LobbySystem.instance.isPauseMenu = true;
        }
    }

    // Called when playing 
    [HarmonyPatch(typeof(MainMenu), "Play")]
    public class MainMenuPlayPatch
    {
        static void Prefix()
        {
            LobbySystem.instance.isPauseMenu = false;
            LobbySystem.instance.isInGame = true;
        }
    }

    // Called when restarting
    [HarmonyPatch(typeof(MainMenu), "Restart")]
    public class MainMenuRestartPatch
    {
        static void Prefix()
        {
            LobbySystem.instance.isPauseMenu = false;
            LobbySystem.instance.isInGame = false;
            LobbySystem.instance.isGameLoaded = false;

            LobbySystem.instance.ExitLobby();
        }
    }

    // Called when exiting
    [HarmonyPatch(typeof(MainMenu), "DoQuit")] //TODO: find another patch / standard method so it exits the lobby in any exit of the application
    public class MainMenuDoQuitPatch
    {
        static void Prefix()
        {
            LobbySystem.instance.ExitLobby();
        }
    }

    public class LobbySystem : MonoBehaviour
    {
        public static LobbySystem instance;
        public Stack<string> GUIStack = new();
        public string JoinLobbyID = string.Empty;
        public CSteamID ActualLobbyID = CSteamID.Nil;
        public int MaxLobbyMembers = 8;
        public string OwnerName;
        public bool isPauseMenu;
        public bool isInGame;
        public bool isGameLoaded;
        public bool inLobby;
        public bool isLobbyOwner;

        // idfk why making dummy vars assigns for the callback but it somehow makes the callbacks be called
        private Callback<LobbyEnter_t> _lobbyEntered;

        public GameObject MainMenu;

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M) && !inLobby)
            {
                if (GUIStack.Count == 0)
                    GUIStack.Push("Main");
                else
                    GUIStack.Clear();
            }
        }

        public List<string> GetLobbyMembers()
        {
            int len = SteamMatchmaking.GetNumLobbyMembers(ActualLobbyID);
            var ret = new List<string>(len);

            for (int i = 0; i < len; i++)
            {
                var member = SteamMatchmaking.GetLobbyMemberByIndex(ActualLobbyID, i);
                ret.Add(SteamFriends.GetFriendPersonaName(new CSteamID(member.m_SteamID)));
            }

            return ret;
        }

        private void OnGUI()
        {
            var lobbyStyle = new GUIStyle(GUI.skin.box);

            if (!isInGame && isGameLoaded && GUIStack.Count != 0)
            {
                GUILayout.BeginArea(new Rect((Screen.width - 220), (Screen.height - 550), 150f, 500), string.Empty);
                GUILayout.BeginVertical(lobbyStyle);
                switch (GUIStack.Peek())
                {
                    case "Main":
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            GUILayout.Label("<color=white>PoPM</color>");
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();

                            GUILayout.Space(15f);

                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("JOIN"))
                                GUIStack.Push("Join");
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();

                            GUILayout.Space(5f);

                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("HOST"))
                                GUIStack.Push("Host");
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                            break;
                        }
                    case "Host":
                        {
                            if (!inLobby)
                            {
                                SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, MaxLobbyMembers);

                                inLobby = true;
                                isLobbyOwner = true;
                            }

                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            GUILayout.Label($"HOST");
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();

                            GUILayout.Space(5f);

                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            GUILayout.Label($"Lobby ID: {ActualLobbyID.GetAccountID()}");

                            if (GUILayout.Button("Copy ID"))
                            {
                                GUIUtility.systemCopyBuffer = ActualLobbyID.GetAccountID().ToString();
                            }

                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();

                            GUILayout.Space(15f);

                            if (GUILayout.Button("<color=#ed0e0e>EXIT</color>"))
                            {
                                ExitLobby();
                                GUIStack.Pop();
                            }

                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            GUILayout.Label($"PLAYERS");
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();

                            GUILayout.Space(5f);

                            foreach (string member in GetLobbyMembers().ToArray())
                            {
                                GUILayout.Label(member);
                            }
                            break;
                        }
                    case "Join":
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            GUILayout.Label($"JOIN");
                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();

                            GUILayout.Space(15f);

                            JoinLobbyID = GUILayout.TextField(JoinLobbyID);

                            GUILayout.Space(5f);

                            if (GUILayout.Button("JOIN"))
                            {
                                if (uint.TryParse(JoinLobbyID, out uint idLong))
                                {
                                    CSteamID lobbyId = new CSteamID(new AccountID_t(idLong), (uint)EChatSteamIDInstanceFlags.k_EChatInstanceFlagLobby | (uint)EChatSteamIDInstanceFlags.k_EChatInstanceFlagMMSLobby, EUniverse.k_EUniversePublic, EAccountType.k_EAccountTypeChat);
                                    SteamMatchmaking.JoinLobby(lobbyId);
                                }
                                inLobby = true;
                                isLobbyOwner = false;
                                GUIStack.Push("Guest");

                                
                            }

                            if (GUILayout.Button("<color=#888888>BACK</color>"))
                            {
                                ExitLobby();
                                GUIStack.Pop();
                            }

                            break;
                        }
                    case "Guest":
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"{OwnerName} Lobby");
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        if (GUILayout.Button("<color=#ed0e0e>EXIT</color>"))
                        {
                            ExitLobby();
                            GUIStack.Pop();
                        }

                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"PLAYERS");
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        GUILayout.Space(5f);

                        foreach (string member in GetLobbyMembers().ToArray())
                        {
                            GUILayout.Label(member);
                        }

                        break;
                }
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }

        public void ExitLobby()
        {
            inLobby = false;
            isLobbyOwner = false;

            Plugin.Logger.LogInfo($"Leaving lobby! {ActualLobbyID.GetAccountID()}");
            SteamMatchmaking.LeaveLobby(ActualLobbyID);

            MainMenu.transform.GetChild(0).GetComponent<Image>().enabled = true;
            MainMenu.transform.GetChild(0).GetComponent<Button>().enabled = true;
            MainMenu.transform.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().color = new Color(1, 1, 1, 1);

            OwnerName = string.Empty;
            ActualLobbyID = CSteamID.Nil;
        }

        public void OnLobbyCreated(LobbyCreated_t pCallback)
        {
            ActualLobbyID = new CSteamID(pCallback.m_ulSteamIDLobby);
            Plugin.Logger.LogInfo($"Created lobby! {ActualLobbyID.GetAccountID()}");
        }

        public void OnLobbyEnter(LobbyEnter_t pCallback)
        {
            ActualLobbyID = new CSteamID(pCallback.m_ulSteamIDLobby);
            Plugin.Logger.LogInfo($"Joined lobby! {ActualLobbyID.GetAccountID()}");

            OwnerName = SteamFriends.GetFriendPersonaName(SteamMatchmaking.GetLobbyOwner(ActualLobbyID));
            Plugin.Logger.LogInfo($"Host ID: {OwnerName}");

            MainMenu.transform.GetChild(0).GetComponent<Image>().enabled = false;
            MainMenu.transform.GetChild(0).GetComponent<Button>().enabled = false;
            MainMenu.transform.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().color = new Color(1, 1, 1, 0.2f);
        }
    }
}
