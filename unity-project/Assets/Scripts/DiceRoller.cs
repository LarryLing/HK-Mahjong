using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DiceRoller : NetworkBehaviour
{
    private const int DICE_COUNT = 2;
    private const float THROW_FORCE = 2.5f;
    private const float ROLL_FORCE = 5f;

    private enum RollState
    {
        WaitingForInput, // waiting for user to roll - input accepted
        Rolling, // dice are rolling - input ignored
        Resolved, // both dice have settled - input ignored until new round starts
    }

    public Dice dicePrefab;

    private readonly NetworkVariable<int> networkRollState = new(
        (int)RollState.WaitingForInput,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public static UnityAction<int[]> OnRollAnnounced;

    public static event UnityAction<int[]> OnRollResolvedServer;

    private RollState State => (RollState)networkRollState.Value;

    private readonly List<Dice> spawnedDice = new();
    private readonly Dictionary<Dice, int> results = new();

    private void Update()
    {
        if (State != RollState.WaitingForInput)
            return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            RequestRollRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestRollRpc(RpcParams rpcParams = default)
    {
        if (State != RollState.WaitingForInput)
            return;

        // TODO: once a turn system exists, check
        // rpcParams.Receive.SenderClientId against "whose turn is it" here
        // and return early if it isn't theirs. Left open since there's no
        // turn manager yet to check against.

        networkRollState.Value = (int)RollState.Rolling;
        StartCoroutine(RollDice());
    }

    private IEnumerator RollDice()
    {
        if (dicePrefab == null)
        {
            networkRollState.Value = (int)RollState.WaitingForInput;
            yield break;
        }

        foreach (Dice die in spawnedDice)
        {
            if (die != null)
            {
                die.OnSettled -= HandleDieSettled;
                NetworkObject netObj = die.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                    netObj.Despawn();
            }
        }

        spawnedDice.Clear();
        results.Clear();

        for (int i = 0; i < DICE_COUNT; i++)
        {
            float xPosition = Random.Range(-1f, 1f);
            float yPosition = Random.Range(1.5f, 2.5f);
            float zPosition = Random.Range(-1f, 1f);
            Vector3 spawnLocation = new(xPosition, yPosition, zPosition);

            float xRotation = Random.Range(0f, 360f);
            float yRotation = Random.Range(0f, 360f);
            float zRotation = Random.Range(0f, 360f);
            Quaternion spawnRotation = Quaternion.Euler(xRotation, yRotation, zRotation);

            Dice dice = Instantiate(dicePrefab, spawnLocation, spawnRotation);
            dice.GetComponent<NetworkObject>().Spawn();

            dice.OnSettled += HandleDieSettled;
            spawnedDice.Add(dice);
            dice.RollDice(THROW_FORCE, ROLL_FORCE, i);

            yield return null;
        }
    }

    private void HandleDieSettled(Dice die, int result)
    {
        results[die] = result;
        if (results.Count < spawnedDice.Count)
            return;

        networkRollState.Value = (int)RollState.Resolved;

        int[] ordered = new int[spawnedDice.Count];
        for (int i = 0; i < spawnedDice.Count; i++)
        {
            ordered[i] = results[spawnedDice[i]];
        }

        OnRollResolvedServer?.Invoke(ordered);
        AnnounceResultRpc(ordered);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void AnnounceResultRpc(int[] results)
    {
        OnRollAnnounced?.Invoke(results);
    }
}
