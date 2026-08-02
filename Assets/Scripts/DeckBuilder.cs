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
    public const int TilesPerStack = 2;
    public const int TotalTileCount = WallCount * StacksPerWall * TilesPerStack; // 144 Tiles
    public const float WallOffsetFromCenter = 2.6f;

    public GameObject tilePrefab;
    public List<Tile> tiles;

    private Deque<GameObject> tileDeque = new();

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        List<GameObject> tileGameObjects = new(TotalTileCount);

        foreach (Tile tile in tiles)
        {
            int copies = tile.category == TileCategory.Flower ? 1 : 4;
            for (int i = 0; i < copies; i++)
            {
                GameObject tileGameObject = Instantiate(tilePrefab);
                TileRenderer tileRenderer = tileGameObject.GetComponent<TileRenderer>();
                tileRenderer.ApplyTexture(tile.texture);

                tileGameObjects.Add(tileGameObject);
            }
        }

        tileGameObjects.Shuffle();

        List<(Vector3 spawnPosition, Quaternion spawnRotation)> calculated =
            GetTileSpawnPositionsAndRotations();

        for (int i = 0; i < TotalTileCount; i++)
        {
            GameObject tileGameObject = tileGameObjects[i];
            tileGameObject.transform.SetPositionAndRotation(
                calculated[i].spawnPosition,
                calculated[i].spawnRotation
            );
            tileDeque.PushBack(tileGameObject);
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
            Quaternion spawnRotation = Quaternion.Euler(180f, wall * 90f, 0f);

            Vector3 alongDirection = spawnRotation * Vector3.right;
            Vector3 outDirection = spawnRotation * Vector3.forward;

            for (int stack = 0; stack < StacksPerWall; stack++)
            {
                float localXPosition = (stack - (StacksPerWall - 1) * 0.5f) * TileWidth - 0.4f;
                Vector3 spawnPosition =
                    outDirection * WallOffsetFromCenter + alongDirection * localXPosition;

                for (int height = 0; height < TilesPerStack; height++)
                {
                    Vector3 yLevel = Vector3.up * (height * TileHeight + TileHeight * 0.5f);
                    calculated.Add((spawnPosition + yLevel, spawnRotation));
                }
            }
        }

        return calculated;
    }
}
