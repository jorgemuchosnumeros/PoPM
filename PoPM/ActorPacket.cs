using UnityEngine;

namespace PoPM
{
    /// <summary>
    /// An actor packet to move and rotate actors
    /// </summary>
    public class ActorPacket
    {
        public Vector3 FacingDirection;

        public ActorStateFlags Flags;
        public int ID;

        public string Name;

        public Vector3 Position;

        public string SteamID;
    }

    public struct ActorStateFlags
    {
        public bool Disconnected;

        public bool Dead;
    }
}