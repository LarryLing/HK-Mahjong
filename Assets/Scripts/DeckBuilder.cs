using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeckBuilder : NetworkBehaviour
{
    public const float TILE_WIDTH = 0.3f; // X-Axis
    public const float TILE_HEIGHT = 0.2f; // Y-Axis
    public const float TILE_LENGTH = 0.4f; // Z-Axis

    public const int WALL_COUNT = 4;
    public const int STACKS_PER_WALL = 18;
    public const int TILES_PER_STACK = 2;
    public const int TILES_PER_WALL = TILES_PER_STACK * STACKS_PER_WALL; // 36 Tiles
    public const int TOTAL_TILE_COUNT = TILES_PER_WALL * WALL_COUNT; // 144 Tiles
    public const float WALL_OFFSET_FROM_CENTER = 2.6f;

    public Tile tilePrefab;
    public List<TileData> tilesData;

    private Deque<Tile> tileDeque = new();

    private void OnEnable()
    {
        DiceRoller.OnRollResolvedServer += HandleDiceRollResolved;
    }

    private void OnDisable()
    {
        DiceRoller.OnRollResolvedServer -= HandleDiceRollResolved;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        List<Tile> tileList = new(TOTAL_TILE_COUNT);

        for (int i = 0; i < tilesData.Count; i++)
        {
            TileData tileData = tilesData[i];

            int copies = tileData.category == TileCategory.Flower ? 1 : 4;

            for (int copy = 0; copy < copies; copy++)
            {
                Tile tile = Instantiate<Tile>(tilePrefab);

                NetworkObject tileNetworkObject = tile.GetComponent<NetworkObject>();
                tileNetworkObject.Spawn();

                tile.tileDataIndex.Value = i;

                tileList.Add(tile);
            }
        }

        tileList.Shuffle();

        List<(Vector3 spawnPosition, Quaternion spawnRotation)> calculated =
            GetTileSpawnPositionsAndRotations();

        for (int i = 0; i < TOTAL_TILE_COUNT; i++)
        {
            Tile tile = tileList[i];
            tile.transform.SetPositionAndRotation(
                calculated[i].spawnPosition,
                calculated[i].spawnRotation
            );
            tileDeque.PushBack(tile);
        }
    }

    private void HandleDiceRollResolved(int[] diceResults)
    {
        int diceSum = 0;
        foreach (int result in diceResults)
            diceSum += result;

        int rotateBy = (diceSum - 1) * -TILES_PER_WALL + diceSum * TILES_PER_STACK;

        tileDeque.Rotate(rotateBy);

        if (tileDeque.TryPeekFront(out Tile frontTile))
        {
            Transform tileTransform = frontTile.GetComponent<Transform>();
            tileTransform.position = new Vector3(
                tileTransform.position.x,
                tileTransform.position.y + 2.5f,
                tileTransform.position.z
            );
        }
    }

    private List<(
        Vector3 spawnPosition,
        Quaternion spawnRotation
    )> GetTileSpawnPositionsAndRotations()
    {
        List<(Vector3 position, Quaternion rotation)> calculated = new();

        for (int wall = 0; wall < WALL_COUNT; wall++)
        {
            Quaternion spawnRotation = Quaternion.Euler(-180f, wall * 90f, 0f);

            Vector3 alongDirection = spawnRotation * -Vector3.right;
            Vector3 outDirection = spawnRotation * Vector3.forward;

            for (int stack = 0; stack < STACKS_PER_WALL; stack++)
            {
                float localXPosition = (stack - (STACKS_PER_WALL - 1) * 0.5f) * TILE_WIDTH + 0.4f;
                Vector3 spawnPosition =
                    outDirection * WALL_OFFSET_FROM_CENTER + alongDirection * localXPosition;

                for (int tileLevel = TILES_PER_STACK - 1; tileLevel >= 0; tileLevel--)
                {
                    Vector3 yLevel = Vector3.up * (tileLevel * TILE_HEIGHT + TILE_HEIGHT * 0.5f);
                    calculated.Add((spawnPosition + yLevel, spawnRotation));
                }
            }
        }

        return calculated;
    }
}
