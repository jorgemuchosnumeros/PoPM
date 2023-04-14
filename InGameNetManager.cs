using System;
using System.Collections.Generic;
using System.Reflection;
using Steamworks;
using UnityEngine;

namespace PoPM;

public class IngameNetManager : MonoBehaviour
{
    public static IngameNetManager Instance;
    
    public List<HSteamNetConnection> serverConnections = new List<HSteamNetConnection>();
    
    /// Server owned
    public HSteamNetPollGroup PollGroup;
    /// Server owned
    
    private Callback<SteamNetConnectionStatusChangedCallback_t> _steamNetConnectionStatusChangedCallback;

    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        SteamNetworkingUtils.InitRelayNetworkAccess();

        _steamNetConnectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatus);
    }

    private void OnConnectionStatus(SteamNetConnectionStatusChangedCallback_t pCallback)
    {
        var info = pCallback.m_info;

        if (info.m_hListenSocket != HSteamListenSocket.Invalid)
        {
            switch (info.m_eState)
                {
                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                        Plugin.Logger.LogInfo($"Connection request from: {info.m_identityRemote.GetSteamID()}");

                        bool inLobby = false;
                        foreach (var memberId in LobbySystem.Instance.GetLobbyMembers())
                        {
                            if (info.m_identityRemote.GetSteamID().ToString() == memberId)
                            {
                                inLobby = true;
                                break;
                            }
                        }

                        if (!inLobby)
                        {
                            SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, 0, null, false);
                            Plugin.Logger.LogError("This user is not part of the lobby! Rejecting the connection.");
                            break;
                        }

                        if (SteamNetworkingSockets.AcceptConnection(pCallback.m_hConn) != EResult.k_EResultOK)
                        {
                            SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, 0, null, false);
                            Plugin.Logger.LogError("Failed to accept connection");
                            break;
                        }

                        serverConnections.Add(pCallback.m_hConn);

                        SteamNetworkingSockets.SetConnectionPollGroup(pCallback.m_hConn, PollGroup);

                        // Unsafe just for the ptr to int.
                        // We are increasing the send buffer size for each connection.
                        unsafe
                        {
                            int _2mb = 2097152;

                            SteamNetworkingUtils.SetConfigValue(ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendBufferSize,
                                ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Connection,
                                (IntPtr)pCallback.m_hConn.m_HSteamNetConnection, ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
                                (IntPtr)(&_2mb));
                        }
                        Plugin.Logger.LogInfo("Accepted the connection");
                        break;

                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                        Plugin.Logger.LogInfo($"Killing connection from {info.m_identityRemote.GetSteamID()}.");
                        SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, 0, null, false);
                        //TODO: Clear NetActors (?)
                        
                        break;
                }
        }
        else
        {
            switch (info.m_eState)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    Plugin.Logger.LogInfo("Connected to server.");
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    Plugin.Logger.LogInfo($"Killing connection from {info.m_identityRemote.GetSteamID()}.");
                    SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, 0, null, false);

                    //MainMenu.Restart();
                    typeof(MainMenu).GetMethod("Restart", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(FindObjectOfType<MainMenu>(), new object[] {});
                    break;
            }
        }
    }
}