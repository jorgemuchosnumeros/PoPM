using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace PoPM
{
    /// <summary>
    /// Shut down the connection if we leave the match.
    /// </summary>
    // TODO: Edit "Restart" text in Pause menu to "Restart (Exit Lobby)"
    [HarmonyPatch(typeof(MainMenu), "Restart")]
    public class OnExitGamePatch
    {
        static void Postfix()
        {
            IngameNetManager.ExitGame();
        }
    }

    [HarmonyPatch(typeof(MenuTransitions), "StopTime")]
    public class StopTimePatch
    {
        static void Prefix()
        {
            if (LobbySystem.Instance.isInLobby)
                return;
        }
    }

    public class IngameNetManager : MonoBehaviour
    {
        public static IngameNetManager Instance;

        /// Server owned
        private static readonly int PACKET_SLACK = 256;

        public float _ticker2 = 0f;
        public int _pps = 0;
        public int _total = 0;
        public int _ppsOut = 0;
        public int _totalOut = 0;
        public int _bytesOut = 0;
        public int _totalBytesOut = 0;

        public bool _showSpecificOutbound;

        public HSteamNetConnection c2SConnection;

        /// Server owned
        public HSteamListenSocket serverSocket;

        public HSteamNetPollGroup pollGroup;

        public List<HSteamNetConnection> serverConnections = new();

        public bool isHost;

        public bool isClient;
        private int _nLanes = 1;
        private SteamNetConnectionRealTimeLaneStatus_t _pLanes;

        public SteamNetConnectionRealTimeStatus_t _realtimeStatus;
        public Dictionary<PacketType, int> _savedBytesOuts = new();
        public Dictionary<PacketType, int> _specificBytesOut = new();

        private bool _startFlowFlag;

        private Callback<SteamNetConnectionStatusChangedCallback_t> _steamNetConnectionStatusChangedCallback;


        public TimedAction MainSendTick = new TimedAction(1.0f / 10);

        public Guid OwnGUID = Guid.NewGuid();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            SteamNetworkingUtils.InitRelayNetworkAccess();

            _steamNetConnectionStatusChangedCallback =
                Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatus);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
                _showSpecificOutbound = !_showSpecificOutbound;

            _ticker2 += Time.deltaTime;

            if (_ticker2 > 1)
            {
                _ticker2 = 0;

                _pps = _total;
                _total = 0;

                _ppsOut = _totalOut;
                _totalOut = 0;

                _bytesOut = _totalBytesOut;
                _totalBytesOut = 0;

                _savedBytesOuts = _specificBytesOut.ToDictionary(entry => entry.Key, entry => entry.Value);
                _specificBytesOut.Clear();
            }
        }

        private void FixedUpdate()
        {
            if (!isClient)
                return;

            SteamNetworkingSockets.RunCallbacks();

            if (isClient)
            {
                var msg_ptr = new IntPtr[PACKET_SLACK];
                int msg_count =
                    SteamNetworkingSockets.ReceiveMessagesOnConnection(c2SConnection, msg_ptr, PACKET_SLACK);

                for (int msg_index = 0; msg_index < msg_count; msg_index++)
                {
                    var msg = Marshal.PtrToStructure<SteamNetworkingMessage_t>(msg_ptr[msg_index]);

                    var msg_data = new byte[msg.m_cbSize];
                    Marshal.Copy(msg.m_pData, msg_data, 0, msg.m_cbSize);

                    using var memStream = new MemoryStream(msg_data);
                    using var packetReader = new ProtocolReader(memStream);

                    var packet = packetReader.ReadPacket();

                    if (packet.Sender != OwnGUID)
                    {
                        _total++;

                        using MemoryStream compressedStream = new MemoryStream(packet.Data);
                        using DeflateStream decompressStream =
                            new DeflateStream(compressedStream, CompressionMode.Decompress);
                        using var dataStream = new ProtocolReader(decompressStream);

                        switch (packet.ID)
                        {
                            case PacketType.ActorUpdate:
                            {
                                var actorPacket = dataStream.ReadActorPacket();

                                if (!actorPacket.Flags.Disconnected)
                                    NetVillager.RegisterClientTransform(actorPacket);
                                else
                                    NetVillager.DestroyVillagerBySteamID(actorPacket.SteamID);

                                break;
                            }

                            case PacketType.GameStateUpdate:
                            {
                                var gameStatePacket = dataStream.ReadGameStatePacket();

                                if (gameStatePacket.EruptionTrigger && !_startFlowFlag)
                                {
                                    NetVillager.Instance.removeBobAnimation = true;
                                    _startFlowFlag = true;
                                    FindObjectOfType<Scr_LavaController>().StartLavaFlow();
                                }

                                break;
                            }
                        }
                    }
                }
            }

            if (isHost)
            {
                var msg_ptr = new IntPtr[PACKET_SLACK];
                int msg_count = SteamNetworkingSockets.ReceiveMessagesOnPollGroup(pollGroup, msg_ptr, PACKET_SLACK);

                for (int msg_index = 0; msg_index < msg_count; msg_index++)
                {
                    var msg = Marshal.PtrToStructure<SteamNetworkingMessage_t>(msg_ptr[msg_index]);

                    for (int i = serverConnections.Count - 1; i >= 0; i--)
                    {
                        var connection = serverConnections[i];

                        if (connection == msg.m_conn)
                            continue;

                        var res = SteamNetworkingSockets.SendMessageToConnection(connection, msg.m_pData,
                            (uint) msg.m_cbSize, Constants.k_nSteamNetworkingSend_Reliable, out long msg_num);

                        if (res != EResult.k_EResultOK)
                        {
                            Plugin.Logger.LogError($"Failure {res}");
                            serverConnections.RemoveAt(i);
                            SteamNetworkingSockets.CloseConnection(connection, 0, null, false);
                        }
                    }

                    Marshal.DestroyStructure<SteamNetworkingMessage_t>(msg_ptr[msg_index]);
                }
            }
        }

        private void LateUpdate()
        {
            if (!isClient)
                return;

            Time.timeScale = 1f;
            Time.fixedDeltaTime = Time.timeScale / 60f;
        }

        private void OnGUI()
        {
            if (!isClient)
                return;

            GUI.Label(new Rect(10, 30, 200, 40), $"Inbound: {_pps} PPS");
            GUI.Label(new Rect(10, 50, 200, 40), $"Outbound: {_ppsOut} PPS -- {_bytesOut} Bytes");

            SteamNetworkingSockets.GetConnectionRealTimeStatus(c2SConnection, ref _realtimeStatus, _nLanes,
                ref _pLanes);
            GUI.Label(new Rect(10, 80, 200, 40), $"Ping: {_realtimeStatus.m_nPing} ms");

            if (_showSpecificOutbound)
            {
                var ordered = _savedBytesOuts.OrderBy(x => -x.Value).ToDictionary(x => x.Key, x => x.Value);
                int i = 0;
                foreach (var kv in ordered)
                {
                    GUI.Label(new Rect(10, 110 + i * 30, 200, 40), $"{kv.Key} - {kv.Value}B");
                    i++;
                }
            }
        }

        public void ResetState()
        {
            _ticker2 = 0f;
            _pps = 0;
            _total = 0;
            _ppsOut = 0;
            _totalOut = 0;
            _bytesOut = 0;
            _totalBytesOut = 0;

            MainSendTick.Start();

            isHost = false;
            isClient = false;

            NetVillager.Instance.removeBobAnimation = false;
        }

        public static void ExitGame()
        {
            if (!Instance.isClient)
                return;

            NetVillager.Instance.SendDisconnect();

            SteamNetworkingSockets.CloseConnection(Instance.c2SConnection, 0, string.Empty, false);

            if (Instance.isHost)
                SteamNetworkingSockets.CloseListenSocket(Instance.serverSocket);

            Instance.ResetState();

            LobbySystem.Instance.isPauseMenu = false;
            LobbySystem.Instance.isInGame = false;
            LobbySystem.Instance.isGameLoaded = false;

            LobbySystem.Instance.ExitLobby();
        }

        public void OpenRelay()
        {
            Plugin.Logger.LogInfo("Starting server socket for connections.");

            serverConnections.Clear();
            serverSocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, null);

            pollGroup = SteamNetworkingSockets.CreatePollGroup();

            isHost = true;
        }

        public void StartAsServer()
        {
            Plugin.Logger.LogInfo("Starting server and client.");

            isHost = true;

            isClient = true;

            var iden = new SteamNetworkingIdentity
            {
                m_eType = ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_SteamID,
            };

            iden.SetSteamID(SteamUser.GetSteamID());

            c2SConnection = SteamNetworkingSockets.ConnectP2P(ref iden, 0, 0, null);
        }

        public void StartAsClient(CSteamID host)
        {
            Plugin.Logger.LogInfo("Starting client.");

            isClient = true;

            var iden = new SteamNetworkingIdentity
            {
                m_eType = ESteamNetworkingIdentityType.k_ESteamNetworkingIdentityType_SteamID,
            };

            iden.SetSteamID(host);

            StartCoroutine(RepeatTryConnect(iden));
        }

        IEnumerator RepeatTryConnect(SteamNetworkingIdentity iden)
        {
            for (int i = 0; i < 30; i++)
            {
                Plugin.Logger.LogInfo($"Attempting connection... {i + 1}/30");

                // Set the initial connection timeout to 2 minutes, for slow hosts.
                SteamNetworkingConfigValue_t timeout = new SteamNetworkingConfigValue_t
                {
                    m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_TimeoutInitial,
                    m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
                    m_val = new SteamNetworkingConfigValue_t.OptionValue {m_int32 = 2 * 60 * 1000},
                };

                c2SConnection =
                    SteamNetworkingSockets.ConnectP2P(ref iden, 0, 1, new SteamNetworkingConfigValue_t[] {timeout});

                if (c2SConnection != HSteamNetConnection.Invalid)
                    yield break;

                yield return new WaitForSeconds(0.5f);
            }
        }

        public void SendPacketToServer(byte[] data, PacketType type, int send_flags)
        {
            _totalOut++;

            using MemoryStream compressOut = new MemoryStream();
            using (DeflateStream deflateStream =
                   new DeflateStream(compressOut, System.IO.Compression.CompressionLevel.Optimal))
            {
                deflateStream.Write(data, 0, data.Length);
            }

            byte[] compressed = compressOut.ToArray();

            using MemoryStream packetStream = new MemoryStream();
            Packet packet = new Packet
            {
                ID = type,
                Sender = OwnGUID,
                Data = compressed
            };

            using (var writer = new ProtocolWriter(packetStream))
            {
                writer.Write(packet);
            }

            byte[] packet_data = packetStream.ToArray();

            _totalBytesOut += packet_data.Length;

            if (_specificBytesOut.ContainsKey(type))
                _specificBytesOut[type] += packet_data.Length;
            else
                _specificBytesOut[type] = packet_data.Length;

            // This is safe. We are only pinning the array.
            unsafe
            {
                fixed (byte* p_msg = packet_data)
                {
                    var res = SteamNetworkingSockets.SendMessageToConnection(c2SConnection, (IntPtr) p_msg,
                        (uint) packet_data.Length, send_flags, out long num);
                    if (res != EResult.k_EResultOK)
                        Plugin.Logger.LogError($"Packet failed to send: {res}");
                }
            }
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
                            if (info.m_identityRemote.GetSteamID() == memberId)
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

                        SteamNetworkingSockets.SetConnectionPollGroup(pCallback.m_hConn, pollGroup);

                        // Unsafe just for the ptr to int.
                        // We are increasing the send buffer size for each connection.
                        unsafe
                        {
                            int _2mb = 2097152;

                            SteamNetworkingUtils.SetConfigValue(
                                ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_SendBufferSize,
                                ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Connection,
                                (IntPtr) pCallback.m_hConn.m_HSteamNetConnection,
                                ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
                                (IntPtr) (&_2mb));
                        }

                        Plugin.Logger.LogInfo("Accepted the connection");
                        break;

                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                    case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    {
                        Plugin.Logger.LogInfo($"Killing connection from {info.m_identityRemote.GetSteamID()}.");
                        SteamNetworkingSockets.CloseConnection(pCallback.m_hConn, 0, null, false);

                        break;
                    }
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

                        LobbySystem.Instance.RestartMenu();

                        break;
                }
            }
        }
    }
}