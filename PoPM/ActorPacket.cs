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

        public string JSONSkin;

        public string Name;

        public Vector3 Position;

        public string SteamID;
    }

    public struct ActorStateFlags
    {
        public bool Disconnected;

        public bool Dead;
    }

    public class CustomVillager
    {
        static System.Random random = new();

        public bool Bottom1 { get; set; } = random.NextDouble() >= 0.5;
        public bool Bottom2 { get; set; } = random.NextDouble() >= 0.5;
        public bool Top1 { get; set; } = random.NextDouble() >= 0.5;
        public bool Top2 { get; set; } = random.NextDouble() >= 0.5;
        public bool LaurelCrown { get; set; } = random.NextDouble() >= 0.5;
        public bool ChinBeard { get; set; } = random.NextDouble() >= 0.5;
        public int Hair { get; set; } = random.Next(0, 18);
        public int HairColor { get; set; } = random.Next(0, 5);
        public int Gender { get; set; } = random.Next(0, 2);
        public int SkinColor { get; set; } = random.Next(0, 4);
    }
}
