using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeckBuilder : NetworkBehaviour
{
    public const float TileWidth = 0.3f; // X-Axis
    public const float TileHeight = 0.2f; // Y-Axis
    public const float TileLength = 0.4f; // Z-Axis

    public const int WallCount = 4;
    public const int StacksPerWall = 18;
    public const int TileLevelsPerStack = 2;
    public const int TilesPerWall = StacksPerWall * TileLevelsPerStack; // 36 Tiles
    public const int TotalTileCount = WallCount * TilesPerWall; // 144 Tiles
    public const float WallOffsetFromCenter = 2.6f;

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

        List<Tile> tileList = new(TotalTileCount);

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

        for (int i = 0; i < TotalTileCount; i++)
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

        int rotateBy = (diceSum - 1) * -TilesPerWall + diceSum * TileLevelsPerStack;

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

        for (int wall = 0; wall < WallCount; wall++)
        {
            Quaternion spawnRotation = Quaternion.Euler(-180f, wall * 90f, 0f);

            Vector3 alongDirection = spawnRotation * -Vector3.right;
            Vector3 outDirection = spawnRotation * Vector3.forward;

            for (int stack = 0; stack < StacksPerWall; stack++)
            {
                float localXPosition = (stack - (StacksPerWall - 1) * 0.5f) * TileWidth + 0.4f;
                Vector3 spawnPosition =
                    outDirection * WallOffsetFromCenter + alongDirection * localXPosition;

                for (int tileLevel = TileLevelsPerStack - 1; tileLevel >= 0; tileLevel--)
                {
                    Vector3 yLevel = Vector3.up * (tileLevel * TileHeight + TileHeight * 0.5f);
                    calculated.Add((spawnPosition + yLevel, spawnRotation));
                }
            }
        }

        return calculated;
    }
}
