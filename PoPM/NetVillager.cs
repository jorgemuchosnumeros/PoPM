using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Steamworks;
using UnityEngine;
using Random = System.Random;

namespace PoPM
{
    [HarmonyPatch(typeof(Scr_LavaController), "StartLavaFlow")]
    public static class SendLavaFlowEventPatch
    {
        static void Prefix()
        {
            using MemoryStream memoryStream = new MemoryStream();
            var gameStatePacket = new GameStatePacket()
            {
                Name = NetVillager.SteamName,
                ID = NetVillager.GameID,
                EruptionTrigger = true,
            };
            using (var writer = new ProtocolWriter(memoryStream))
            {
                writer.Write(gameStatePacket);
            }

            byte[] data = memoryStream.ToArray();

            IngameNetManager.Instance.SendPacketToServer(data, PacketType.GameStateUpdate,
                Constants.k_nSteamNetworkingSend_Reliable);
        }
    }

    [HarmonyPatch(typeof(PlayerDeath), nameof(PlayerDeath.Trigger))]
    public static class RemoveBobPatch
    {
        static void Postfix(PlayerDeath __instance)
        {
            if (NetVillager.Instance.removeBobAnimation)
            {
                __instance.gameObject.SetActive(false);
                var lavaSplash = (GameObject) typeof(PlayerDeath)
                    .GetField("lavaSplash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                lavaSplash.SetActive(false);

                var fire = (ParticleSystem) typeof(PlayerDeath)
                    .GetField("fire", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
                fire.Stop();
            }
        }
    }

    public class NetVillager : MonoBehaviour
    {
        public static NetVillager Instance;
        public static string SteamName;
        public static CSteamID SteamID = CSteamID.Nil;
        public static int GameID;

        public static Dictionary<int, GameObject> NetVillagerTargets = new();
        public static Dictionary<int, GameObject> NetVillagers = new();
        public static Dictionary<CSteamID, int> NetVillagersSteamID2GameID = new();

        private static GameObject _defaultVillager;

        public Transform fpsTransform;
        public Camera fpsCamera;

        public bool removeBobAnimation;

        private readonly TimedAction _mainSendTick = new(1.0f / 10);
        private Vector3 _position;

        private Random _randomGen = new();
        private Quaternion _rotation;


        private void Start()
        {
            NetVillagerTargets = new();
            NetVillagers = new();
            NetVillagersSteamID2GameID = new();

            Instance = this;

            _mainSendTick.Start();

            _defaultVillager = GameObject.Find("Starting area/Villagers/Villager (3)");

            GameID = _randomGen.Next(13337, int.MaxValue);
            SteamName = SteamFriends.GetPersonaName();
            SteamID = SteamUser.GetSteamID();

            fpsCamera = fpsTransform.GetComponent<Camera>();
        }

        private void Update()
        {
            if (_mainSendTick.TrueDone())
            {
                SendPositionAndRotation();

                _mainSendTick.Start();
            }

            foreach (var netVillager in NetVillagers)
            {
                if (netVillager.Value == null)
                {
                    // TODO: Idk
                    return;
                }

                netVillager.Value.transform.position = (netVillager.Value.transform.position -
                                                        NetVillagerTargets[netVillager.Key].transform.position)
                    .magnitude > 5
                        ? NetVillagerTargets[netVillager.Key].transform.position
                        : Vector3.Lerp(netVillager.Value.transform.position,
                            NetVillagerTargets[netVillager.Key].transform.position, 10f * Time.deltaTime);

                netVillager.Value.transform.rotation = Quaternion.Slerp(netVillager.Value.transform.rotation,
                    NetVillagerTargets[netVillager.Key].transform.rotation, 5f * Time.deltaTime);
            }
        }

        public static void GetOwnTransform()
        {
            var playerInfoSender = new GameObject();
            playerInfoSender.AddComponent<NetVillager>();
            playerInfoSender.GetComponent<NetVillager>().fpsTransform =
                GameObject.Find("FPSController/FirstPersonCharacter").transform;
        }

        public static void RegisterClientTransform(ActorPacket actorPacket)
        {
            if (!NetVillagerTargets.ContainsKey(actorPacket.ID))
            {
                Plugin.Logger.LogInfo(
                    $"New Villager (Player) instantiated with name: {actorPacket.Name}, {actorPacket.SteamID} id: {actorPacket.ID}");

                NetVillagerTargets.Add(actorPacket.ID, new GameObject());

                GameObject villager = Instantiate(_defaultVillager);

                villager.AddComponent<NameTag>().GetComponent<NameTag>().nameTagText = actorPacket.Name;

                NetVillagers.Add(actorPacket.ID, villager);

                NetVillagersSteamID2GameID.Add(new CSteamID(Convert.ToUInt64(actorPacket.SteamID)), actorPacket.ID);
            }

            foreach (var target in NetVillagerTargets)
            {
                if (target.Key == actorPacket.ID)
                {
                    target.Value.transform.position =
                        actorPacket.Position - new Vector3(0, 1.6f, 0); // To ground offset;

                    var eulerAngles = target.Value.transform.eulerAngles;
                    target.Value.transform.rotation =
                        Quaternion.Euler(eulerAngles.x, actorPacket.FacingDirection.y, eulerAngles.z);
                }
            }
        }

        public void SendDisconnect()
        {
            using MemoryStream memoryStream = new MemoryStream();
            var actorPacket = new ActorPacket
            {
                ID = GameID,
                Name = SteamName,
                SteamID = SteamID.m_SteamID.ToString(),
                Flags = new ActorStateFlags
                {
                    Disconnected = true,
                },
            };
            using (var writer = new ProtocolWriter(memoryStream))
            {
                writer.Write(actorPacket);
            }

            byte[] data = memoryStream.ToArray();

            IngameNetManager.Instance.SendPacketToServer(data, PacketType.ActorUpdate,
                Constants.k_nSteamNetworkingSend_Reliable);
        }

        private void SendPositionAndRotation()
        {
            var prevPosition = _position;
            var prevRotation = _rotation;
            _position = fpsTransform.position;
            _rotation = fpsTransform.rotation;

            // If we dont move or we are paused, dont bother on sending the position
            if ((_position == prevPosition && _rotation == prevRotation) || LobbySystem.Instance.isPauseMenu)
                return;

            using MemoryStream memoryStream = new MemoryStream();
            var actorPacket = new ActorPacket
            {
                Name = SteamName,
                ID = GameID,
                SteamID = SteamID.m_SteamID.ToString(),
                Position = fpsTransform.position,
                FacingDirection = fpsTransform.eulerAngles,
            };
            using (var writer = new ProtocolWriter(memoryStream))
            {
                writer.Write(actorPacket);
            }

            byte[] data = memoryStream.ToArray();

            IngameNetManager.Instance.SendPacketToServer(data, PacketType.ActorUpdate,
                Constants.k_nSteamNetworkingSend_Unreliable);
        }

        public static void DestroyVillagerBySteamID(string steamID)
        {
            CSteamID csteamID = new CSteamID(Convert.ToUInt64(steamID));

            var ID = NetVillagersSteamID2GameID[csteamID];

            Plugin.Logger.LogInfo("Destroy");
            try
            {
                Destroy(NetVillagers[ID].GetComponent<NameTag>().textInstance);
                Destroy(NetVillagers[ID]);
                NetVillagers.Remove(ID);
                Destroy(NetVillagerTargets[ID]);
                NetVillagerTargets.Remove(ID);

                NetVillagersSteamID2GameID.Remove(csteamID);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError($"{steamID}: {e}");
            }
        }
    }
}