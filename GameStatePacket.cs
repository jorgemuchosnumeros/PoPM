namespace PoPM
{
    /// <summary>
    /// The GameStatePacket sends common events, like the volcanic eruption and other details
    /// </summary>
    public class GameStatePacket
    {
        public int ID;

        public string Name;

        public bool EruptionTrigger;
    }
}