using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Tile : NetworkBehaviour
{
    public NetworkVariable<int> tileDataIndex = new();

    [SerializeField]
    private List<TileData> tilesData;

    public TileData TileData => tilesData[tileDataIndex.Value];

    private Renderer meshRenderer;
    private MaterialPropertyBlock _propBlock;

    private void Awake()
    {
        meshRenderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    public override void OnNetworkSpawn()
    {
        tileDataIndex.OnValueChanged += (_, _) => ApplyTexture();
        ApplyTexture();
    }

    private void ApplyTexture()
    {
        meshRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetTexture("_BaseMap", TileData.texture);
        meshRenderer.SetPropertyBlock(_propBlock);
    }
}
