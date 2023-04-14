using System;
using System.Reflection;
using HarmonyLib;
using Steamworks;
using UnityEngine;


namespace PoPM
{
    /// <summary>
    /// The purpose of this file is to set all the necessary patches to force PoP to use the newer Steamworks.NET
    /// since the one is currently using doesnt support the steam relay services.
    /// </summary>

    //TODO: Get the new Steamworks.NET running without breaking the  Steam Overlay and/or the Achievements
    
    [HarmonyPatch(typeof(SteamManager), "Awake")]
    public class SteamManagerAwakePatch
    {
        static void Prefix(SteamManager __instance)
        {
            typeof(SteamManager).GetField("m_bInitialized", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(__instance, SteamAPI.Init());
            return;
        }
    }
}