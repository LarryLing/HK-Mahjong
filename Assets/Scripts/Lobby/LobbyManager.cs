using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    private const float HeartbeatTimerMax = 15f;
    private const int RequiredPlayers = 4;

    private ILobbyEvents lobbyEvents;
    private Lobby hostLobby;
    private Lobby joinedLobby;

    private float heartbeatTimer;

    private string playerName;
    private string playerId;

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

        heartbeatTimer = HeartbeatTimerMax;

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
                        "MinimumPlayers",
                        new DataObject(
                            DataObject.VisibilityOptions.Member,
                            RequiredPlayers.ToString()
                        )
                    },
                    { "MinimumPoints", new DataObject(DataObject.VisibilityOptions.Member, "0") },
                    { "TurnDuration", new DataObject(DataObject.VisibilityOptions.Member, "60") },
                },
            };

            hostLobby = joinedLobby = await LobbyService.Instance.CreateLobbyAsync(
                lobbyName,
                RequiredPlayers,
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
                    "PlayerName",
                    new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName)
                },
                {
                    "IsReady",
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
        if (IsHost(playerId) == false)
            return;

        try
        {
            UpdateLobbyOptions updateLobbyPointRequirementOptions = new()
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "MinimumPoints",
                        new DataObject(DataObject.VisibilityOptions.Member, newMinimumPoints)
                    },
                },
            };

            hostLobby = await LobbyService.Instance.UpdateLobbyAsync(
                hostLobby.Id,
                updateLobbyPointRequirementOptions
            );
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update point requirement: {e.Message}");
        }
    }

    private async void UpdateLobbyTurnDuration(string newTurnDuration)
    {
        if (IsHost(playerId) == false)
            return;

        try
        {
            UpdateLobbyOptions updateLobbyTurnDurationOptions = new()
            {
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "TurnDuration",
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

    private async void UpdatePlayerName(string newPlayerName)
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
                        "PlayerName",
                        new PlayerDataObject(
                            PlayerDataObject.VisibilityOptions.Member,
                            newPlayerName
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
            Debug.LogError($"Failed to update player name: {e.Message}");
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
                        "IsReady",
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
        if (IsHost(playerId) == false || IsLobbyMember(kickedPlayerId) == false)
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
        if (IsHost(playerId) == false || IsLobbyMember(newHostId) == false || playerId == newHostId)
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
        if (IsHost(playerId) == false)
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

    private bool IsHost(string playerId)
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
