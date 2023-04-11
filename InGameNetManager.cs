using Steamworks;
using UnityEngine;

namespace PoPM;

public class IngameNetManager : MonoBehaviour
{
    public static IngameNetManager Instance;

    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        //TODO: SteamNetworkingUtils not found in this firstpass
        //SteamNetworkingUtils.InitRelayNetworkAccess();

        //Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatus);
    }
}