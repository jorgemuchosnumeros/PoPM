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
                Name = NetVillager.steamName,
                ID = NetVillager.id,
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
            if (NetVillager.Instance.removeBobAnimation) //TODO: Make it actually work
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
        public static string steamName;
        public static int id;

        public static Dictionary<int, GameObject> NetVillagerTargets = new();
        public static Dictionary<int, GameObject> NetVillagers = new();
        public static Dictionary<string, int> NetVillagersSteamName2ID = new();

        private static GameObject _defaultVillager = null;

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
            NetVillagersSteamName2ID = new();

            Instance = this;

            _mainSendTick.Start();

            _defaultVillager = GameObject.Find("Starting area/Villagers/Villager (3)");

            id = _randomGen.Next(13337, int.MaxValue);
            steamName = SteamFriends.GetPersonaName();

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
                    $"New Villager (Player) instantiated with name: {actorPacket.Name} id: {actorPacket.ID}");

                NetVillagerTargets.Add(actorPacket.ID, new GameObject());

                GameObject villager = Instantiate(_defaultVillager);

                villager.AddComponent<NameTag>().GetComponent<NameTag>().name = actorPacket.Name;

                NetVillagers.Add(actorPacket.ID, villager);
                NetVillagersSteamName2ID.Add(actorPacket.Name, actorPacket.ID);
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
                Name = steamName,
                ID = id,
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

        public void DestroyVillagerByName(string SteamName)
        {
            var ID = NetVillagersSteamName2ID[SteamName];

            try
            {
                var nameTag = NetVillagers[ID].GetComponent<NameTag>();
                Destroy(nameTag.textInstance);
                Destroy(nameTag);

                Destroy(NetVillagers[ID]);
                NetVillagers.Remove(ID);
                Destroy(NetVillagerTargets[ID]);
                NetVillagerTargets.Remove(ID);

                NetVillagersSteamName2ID.Remove(SteamName);
            }
            catch (Exception e)
            {
                Plugin.Logger.LogError($"{SteamName}: {e}");
            }
        }
    }
}