using System;
using System.Collections.Generic;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace PoPM;

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

public class LobbySystem: MonoBehaviour
{
    public static LobbySystem instance;
    public Stack<string> GUIStack = new();
    public string JoinLobbyID = string.Empty;
    public CSteamID ActualLobbyID = CSteamID.Nil;
    public bool isPauseMenu;
    public bool isInGame;
    public bool isGameLoaded;
    public bool inLobby;
    public bool isLobbyOwner;
    
    

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Callback<LobbyEnter_t>.Create(OnLobbyEnter);
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

    private void OnGUI()
    {
        var lobbyStyle = new GUIStyle(GUI.skin.box);

        if (!isInGame && isGameLoaded && GUIStack.Count != 0)
        {
            GUILayout.BeginArea(new Rect((Screen.width - 220), (Screen.height - 550), 150f, 200f), string.Empty);
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
                    GUILayout.Label($"Lobby ID: {ActualLobbyID.GetAccountID().ToString()}");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    
                    GUILayout.Space(15f);
                    
                    if (GUILayout.Button("<color=#ed0e0e>EXIT</color>"))
                    {
                        ExitLobby();
                        GUIStack.Pop();
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
                        throw new NotImplementedException();
                
                    if (GUILayout.Button("<color=#888888>BACK</color>"))
                        GUIStack.Pop();
                    break;
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }

    private void ExitLobby()
    {
        inLobby = false;
        isLobbyOwner = false;
        Plugin.Logger.LogInfo($"Leaving lobby! {ActualLobbyID.GetAccountID().ToString()}");
        SteamMatchmaking.LeaveLobby(ActualLobbyID);
    }

    private void OnLobbyEnter(LobbyEnter_t pCallback)
    {
        ActualLobbyID = new CSteamID(pCallback.m_ulSteamIDLobby);
        Plugin.Logger.LogInfo($"Joined lobby! {ActualLobbyID.GetAccountID().ToString()}");
    }
}