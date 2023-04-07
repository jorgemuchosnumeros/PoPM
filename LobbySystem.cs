using System;
using HarmonyLib;
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
    public bool isPauseMenu;
    public bool isInGame;
    public bool isGameLoaded;
    

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        //
    }

    private void OnGUI()
    {
        var lobbyStyle = new GUIStyle(GUI.skin.box);

        if (!isInGame && isGameLoaded)
        {
            GUILayout.BeginArea(new Rect((Screen.width - 220), (Screen.height - 550), 150f, 200f), string.Empty);
            GUILayout.BeginVertical(lobbyStyle);
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("<color=white>PoPM</color>");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(15f);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("JOIN"))
               Plugin.Logger.LogInfo("Display join menu");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5f);
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("HOST"))
                Plugin.Logger.LogInfo("Display host menu");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}