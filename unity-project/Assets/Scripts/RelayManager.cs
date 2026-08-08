using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Error is caught in LobbyManager.StartGame() function
    public async Task<string> CreateRelay()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(
            LobbyManager.REQUIRED_PLAYER_COUNT - 1
        );

        string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(
            allocation.AllocationId
        );

        RelayServerData relayServerData = AllocationUtils.ToRelayServerData(allocation, "wss");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        NetworkManager.Singleton.StartHost();

        return relayJoinCode;
    }

    public async void JoinRelay(string relayJoinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(
                relayJoinCode
            );

            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(
                joinAllocation,
                "wss"
            );
            NetworkManager
                .Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to join relay: {e.Message}");
        }
    }
}
