using System.Collections;
using System.Collections.Generic;
using System.IO;
using Steamworks;
using UnityEngine;
using Random = System.Random;

namespace PoPM
{
    public class NetVillager: MonoBehaviour
    {
        public Transform fpsTransform;
        private Quaternion _rotation;
        private Vector3 _position;
        private string _steamName;
        private int _id;
        
        public static Dictionary<int, GameObject> NetVillagerTargets = new();
        public static Dictionary<int, GameObject> NetVillagers = new();

        private static GameObject _defaultVillager;
        
        private Random _randomGen = new();
        private readonly TimedAction _mainSendTick = new(1.0f / 10);


        private void Start()
        {
            _mainSendTick.Start();
            _defaultVillager = GameObject.Find("Starting area/Villagers/Villager (3)");

            _id = _randomGen.Next(13337, int.MaxValue);
            _steamName = SteamFriends.GetPersonaName();
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
                netVillager.Value.transform.position = (netVillager.Value.transform.position - NetVillagerTargets[netVillager.Key].transform.position).magnitude > 5
                    ? NetVillagerTargets[netVillager.Key].transform.position
                    : Vector3.Lerp(netVillager.Value.transform.position, NetVillagerTargets[netVillager.Key].transform.position, 10f * Time.deltaTime);

                netVillager.Value.transform.rotation = Quaternion.Slerp(netVillager.Value.transform.rotation, NetVillagerTargets[netVillager.Key].transform.rotation, 5f * Time.deltaTime);
            }
        }

        public static void GetOwnTransform()
        {
            var playerInfoSender = new GameObject();
            playerInfoSender.AddComponent<NetVillager>();
            playerInfoSender.GetComponent<NetVillager>().fpsTransform = GameObject.Find("FPSController/FirstPersonCharacter").transform;
        }

        public static void RegisterClientTransform(ActorPacket actorPacket)
        {
            if (!NetVillagerTargets.ContainsKey(actorPacket.ID))
            {
                Plugin.Logger.LogInfo($"New Villager (Player) instantiated with name: {actorPacket.Name} id: {actorPacket.ID}");
                
                NetVillagerTargets.Add(actorPacket.ID, new GameObject());
                
                GameObject villager = Instantiate<GameObject>(_defaultVillager) as GameObject;
                NetVillagers.Add(actorPacket.ID, villager);
            }
            
            foreach (var target in NetVillagerTargets)
            {
                if (target.Key == actorPacket.ID)
                {
                    target.Value.transform.position = actorPacket.Position - new Vector3(0, 1.6f, 0); // To ground offset;

                    var eulerAngles = target.Value.transform.eulerAngles;
                    target.Value.transform.rotation = Quaternion.Euler(eulerAngles.x, actorPacket.FacingDirection.y, eulerAngles.z);
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
            var testPacket = new ActorPacket
            {
                Name = _steamName,
                ID = _id,
                Position = fpsTransform.position,
                FacingDirection = fpsTransform.eulerAngles,
            };
            using (var writer = new ProtocolWriter(memoryStream))
            {
                writer.Write(testPacket);
            }

            byte[] data = memoryStream.ToArray();

            IngameNetManager.Instance.SendPacketToServer(data, PacketType.ActorUpdate, Constants.k_nSteamNetworkingSend_Reliable);
        }
    }    
}
