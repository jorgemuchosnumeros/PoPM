using System;
using System.Collections.Generic;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace PoPM
{
    // Called every time in menu
    [HarmonyPatch(typeof(MainMenu), "Initialize")]
    public class MainMenuInitializePatch
    {
        static void Prefix()
        {
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
        }
    }

    public class LobbySystem : MonoBehaviour
    {
        public static LobbySystem instance;
        public Stack<string> GUIStack = new();
        public string JoinLobbyID = string.Empty;
        public CSteamID ActualLobbyID = CSteamID.Nil;
        public string OwnerName;
        public bool isPauseMenu;
        public bool isInGame;
        public bool isGameLoaded;
        public bool inLobby;
        public bool isLobbyOwner;

        // idfk why making dummy vars assigns for the callback but it somehow makes the callbacks be called
        private Callback<LobbyCreated_t> _lobbyCreated;
        private Callback<LobbyEnter_t> _lobbyEntered;

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
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
                GUILayout.BeginArea(new Rect((Screen.width - 220), (Screen.height - 550), 150f, 500f), string.Empty);
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
                                SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, 4);

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

                            GUILayout.FlexibleSpace();
                            foreach (string elemento in GetLobbyMembers().ToArray())
                            {
                                GUILayout.Label(elemento);
                            }
                            GUILayout.FlexibleSpace();
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

                            if (GUILayout.Button("<color=#ed0e0e>EXIT</color>"))
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
        }
    }
}
