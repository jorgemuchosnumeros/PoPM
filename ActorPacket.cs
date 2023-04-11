using System.Collections.Generic;
using UnityEngine;

namespace PoPM
{
    /// <summary>
    /// An actor packet to move and rotate actors
    /// </summary>
    public class ActorPacket
    {
        public int ID;

        public string Name;

        public Vector3 Position;

        public Vector3 FacingDirection;
    }
    
    public class BulkActorUpdate
    {
        public List<ActorPacket> Updates;
    }
}