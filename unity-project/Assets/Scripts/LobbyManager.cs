using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    private const float HEARTBEAT_TIMER_MAX = 15f;
    public const int REQUIRED_PLAYER_COUNT = 4;

    public const string PLAYER_DATA_KEY_PLAYER_NAME = "PlayerName";
    public const string PLAYER_DATA_KEY_IS_READY = "IsReady";

    public const string LOBBY_DATA_KEY_MINIMUM_PLAYERS = "MinimumPlayers";
    public const string LOBBY_DATA_KEY_MINIMUM_POINTS = "MinimumPoints";
    public const string LOBBY_DATA_KEY_TURN_DURATION = "TurnDuration";
    public const string LOBBY_DATA_KEY_RELAY_JOIN_CODE = "RelayJoinCode";

    public const string LOBBY_DATA_DEFAULT_VALUE_MINIMUM_POINTS = "0";
    public const string LOBBY_DATA_DEFAULT_VALUE_TURN_DURATION = "60";
    public const string LOBBY_DATA_DEFAULT_VALUE_RELAY_JOIN_CODE = "0";

    private ILobbyEvents lobbyEvents;
    private Lobby hostLobby;
    private Lobby joinedLobby;

    private float heartbeatTimer;

    public string playerName { get; set; }
    public string playerId { get; set; }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
                Debug.Log("Unity Services successfully initialized.");
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                playerName = "TestPlayer" + UnityEngine.Random.Range(1, 99);
                playerId = AuthenticationService.Instance.PlayerId;

                Debug.Log(
                    $"Player authenticated. Player Name: {playerName} | Player ID: {playerId}"
                );
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Initialization/Authentication failed: {e.Message}");
        }
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
    }

    private async void OnDestroy()
    {
        if (lobbyEvents == null)
            return;

        await lobbyEvents.UnsubscribeAsync();
    }

    private async void HandleLobbyHeartbeat()
    {
        if (hostLobby == null)
            return;

        heartbeatTimer -= Time.deltaTime;

        if (heartbeatTimer > 0f)
            return;

        heartbeatTimer = HEARTBEAT_TIMER_MAX;

        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            Debug.Log("Sent lobby heartbeat ping.");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to send heartbeat: {e.Message}");
        }
    }

    private async void CreateLobby()
    {
        if (hostLobby != null)
            return;

        try
        {
            string lobbyName = $"{playerName}'s Game";

            CreateLobbyOptions createLobbyOptions = new()
            {
                IsPrivate = true,
                Player = GetPlayerObject(),
                Data = new Dictionary<string, DataObject>
                {
                    {
                        LOBBY_DATA_KEY_MINIMUM_PLAYERS,
                        new DataObject(
                            DataObject.VisibilityOptions.Member,
                            REQUIRED_PLAYER_COUNT.ToString()
                        )
                    },
                    {
                        LOBBY_DATA_KEY_MINIMUM_POINTS,
                        new DataObject(
                            DataObject.VisibilityOptions.Member,
                            LOBBY_DATA_DEFAULT_VALUE_MINIMUM_POINTS
                        )
                    },
                    {
                        LOBBY_DATA_KEY_TURN_DURATION,
                        new DataObject(
                            DataObject.VisibilityOptions.Member,
                            LOBBY_DATA_DEFAULT_VALUE_TURN_DURATION
                        )
                    },
                    {
                        LOBBY_DATA_KEY_RELAY_JOIN_CODE,
                        new DataObject(
                            DataObject.VisibilityOptions.Member,
                            LOBBY_DATA_DEFAULT_VALUE_RELAY_JOIN_CODE
                        )
                    },
                },
            };

            hostLobby = joinedLobby = await LobbyService.Instance.CreateLobbyAsync(
                lobbyName,
                REQUIRED_PLAYER_COUNT,
                createLobbyOptions
            );

            await SubscribeToLobbyChanges(hostLobby.Id);

            Debug.Log($"Lobby successfully created!");
            Debug.Log($"Name: {hostLobby.Name} | Max Players: {hostLobby.MaxPlayers}");
            Debug.Log($"Lobby ID: {hostLobby.Id} | Lobby Code: {hostLobby.LobbyCode}");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to create lobby: {e.Message}");
        }
    }

    private async void JoinPrivateLobby(string joinCode)
    {
        if (joinedLobby != null)
            return;

        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new() { Player = GetPlayerObject() };

            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(
                joinCode,
                joinLobbyByCodeOptions
            );

            await SubscribeToLobbyChanges(joinedLobby.Id);

            Debug.Log($"Joined lobby successfully!");
            Debug.Log(
                $"Name: {joinedLobby.Name} | Lobby ID: {joinedLobby.Id} | Lobby Code: {joinedLobby.LobbyCode} | Player Count: {joinedLobby.Players.Count}"
            );
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");
        }
    }

    private async Task SubscribeToLobbyChanges(string lobbyId)
    {
        try
        {
            LobbyEventCallbacks callbacks = new();

            callbacks.LobbyChanged += OnLobbyChanged;
            callbacks.LobbyDeleted += OnLobbyDeleted;

            callbacks.PlayerJoined += OnPlayerJoined;
            callbacks.PlayerLeft += OnPlayerLeft;
            callbacks.PlayerDataChanged += OnPlayerDataChanged;

            callbacks.KickedFromLobby += OnKickedFromLobby;

            lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(
                lobbyId,
                callbacks
            );
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to subscribe to lobby events: {e.Message}");
        }
    }

    private void OnLobbyChanged(ILobbyChanges changes)
    {
        if (changes.LobbyDeleted)
        {
            Debug.Log("The lobby was deleted by the host.");
            return;
        }

        changes.ApplyToLobby(joinedLobby);

        if (changes.Data.Changed)
        {
            if (changes.Data.Value.TryGetValue(LOBBY_DATA_KEY_RELAY_JOIN_CODE, out var relayChange))
            {
                if (IsLobbyHost(playerId))
                    return;

                string relayJoinCode = relayChange.Value.Value;
                if (relayJoinCode != LOBBY_DATA_DEFAULT_VALUE_RELAY_JOIN_CODE)
                {
                    RelayManager.Instance.JoinRelay(relayJoinCode);
                    return;
                }
            }

            UpdateLobbyDataUI();
            Debug.Log("Updating local lobby UI");
        }

        if (changes.HostId.Changed)
        {
            if (changes.HostId.Value == playerId)
            {
                hostLobby = joinedLobby;

                throw new NotImplementedException("TODO: display host UI");
            }
            else
            {
                hostLobby = null;
                throw new NotImplementedException("TODO: display member UI");
            }
        }
    }

    private void OnLobbyDeleted()
    {
        hostLobby = null;
        joinedLobby = null;
        lobbyEvents = null;
    }

    private void OnKickedFromLobby()
    {
        lobbyEvents = null;
        joinedLobby = null;

        throw new NotImplementedException("TODO: bring player back to main menu");
    }

    private void OnPlayerJoined(List<LobbyPlayerJoined> playersJoined)
    {
        throw new NotImplementedException("TODO: update player list UI");
    }

    private void OnPlayerLeft(List<int> playerIndexesLeft)
    {
        throw new NotImplementedException("TODO: update player list UI");
    }

    private void OnPlayerDataChanged(
        Dictionary<
            int,
            Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>
        > changedData
    )
    {
        throw new NotImplementedException("TODO: update player list UI");
    }

    private void UpdateLobbyDataUI()
    {
        throw new NotImplementedException("TODO: update lobby data UI");
    }

    private Player GetPlayerObject()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                {
                    PLAYER_DATA_KEY_PLAYER_NAME,
                    new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)
                },
                {
                    PLAYER_DATA_KEY_IS_READY,
                    new PlayerDataObject(
                        PlayerDataObject.VisibilityOptions.Member,
                        false.ToString()
                    )
                },
            },
        };
    }

    private async void UpdateLobbyMinimumPoints(string newMinimumPoints)
    {
        if (IsLobbyHost(playerId) == false)
            return;

        try
        {
            UpdateLobbyOptions updateLobbyMinimumPointsOptions = new()
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        LOBBY_DATA_KEY_MINIMUM_POINTS,
                        new DataObject(DataObject.VisibilityOptions.Member, newMinimumPoints)
                    },
                },
            };

            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(
                hostLobby.Id,
                updateLobbyMinimumPointsOptions
            );
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update minimum points: {e.Message}");
        }
    }

    private async void UpdateLobbyTurnDuration(string newTurnDuration)
    {
        if (IsLobbyHost(playerId) == false)
            return;

        try
        {
            UpdateLobbyOptions updateLobbyTurnDurationOptions = new()
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        LOBBY_DATA_KEY_TURN_DURATION,
                        new DataObject(DataObject.VisibilityOptions.Member, newTurnDuration)
                    },
                },
            };

            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(
                hostLobby.Id,
                updateLobbyTurnDurationOptions
            );
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update turn duration: {e.Message}");
        }
    }

    private async void UpdatePlayerStatus(bool isReady)
    {
        if (IsLobbyMember(playerId) == false)
            return;

        try
        {
            UpdatePlayerOptions updatePlayerOptions = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    {
                        PLAYER_DATA_KEY_IS_READY,
                        new PlayerDataObject(
                            PlayerDataObject.VisibilityOptions.Member,
                            isReady.ToString()
                        )
                    },
                },
            };

            await LobbyService.Instance.UpdatePlayerAsync(
                joinedLobby.Id,
                playerId,
                updatePlayerOptions
            );
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update player status: {e.Message}");
        }
    }

    private async void LeaveLobby()
    {
        if (IsLobbyMember(playerId) == false)
            return;

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            await lobbyEvents.UnsubscribeAsync();

            hostLobby = null;
            joinedLobby = null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to leave lobby: {e.Message}");
        }
    }

    private async void KickPlayer(string kickedPlayerId)
    {
        if (IsLobbyHost(playerId) == false || IsLobbyMember(kickedPlayerId) == false)
            return;

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(hostLobby.Id, kickedPlayerId);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to kick player: {e.Message}");
        }
    }

    private async void UpdateLobbyHost(string newHostId)
    {
        if (
            IsLobbyHost(playerId) == false
            || IsLobbyMember(newHostId) == false
            || playerId == newHostId
        )
            return;

        try
        {
            UpdateLobbyOptions updateLobbyHostOptions = new() { HostId = newHostId };

            joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(
                hostLobby.Id,
                updateLobbyHostOptions
            );

            hostLobby = null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update lobby host: {e.Message}");
        }
    }

    private async void DeleteLobby()
    {
        if (IsLobbyHost(playerId) == false)
            return;

        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(hostLobby.Id);
            await lobbyEvents.UnsubscribeAsync();

            hostLobby = null;
            joinedLobby = null;
            lobbyEvents = null;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to delete lobby: {e.Message}");
        }
    }

    private async void StartGame()
    {
        if (IsLobbyHost(playerId) == false)
            return;

        try
        {
            string relayJoinCode = await RelayManager.Instance.CreateRelay();

            UpdateLobbyOptions updateLobbyOptions = new()
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        LOBBY_DATA_KEY_RELAY_JOIN_CODE,
                        new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)
                    },
                },
            };

            joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(
                joinedLobby.Id,
                updateLobbyOptions
            );
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to start game (lobby): {e.Message}");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to start game (relay): {e.Message}");
        }
    }

    private bool IsLobbyHost(string playerId)
    {
        if (hostLobby == null)
            return false;

        return hostLobby.HostId == playerId;
    }

    private bool IsLobbyMember(string playerId)
    {
        if (joinedLobby == null)
            return false;

        foreach (Player player in joinedLobby.Players)
        {
            if (player.Id == playerId)
            {
                return true;
            }
        }

        return false;
    }
}
