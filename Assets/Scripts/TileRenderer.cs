using UnityEngine;

public class TileRenderer : MonoBehaviour
{
    [SerializeField]
    private Renderer meshRenderer;
    private MaterialPropertyBlock _propBlock;

    private void Awake()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<Renderer>();
        }

        _propBlock = new MaterialPropertyBlock();
    }

    public void ApplyTexture(Texture2D texture)
    {
        meshRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetTexture("_BaseMap", texture);
        meshRenderer.SetPropertyBlock(_propBlock);
    }
}
