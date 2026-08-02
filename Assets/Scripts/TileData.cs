using UnityEngine;

public enum TileCategory
{
    Suit,
    Honor,
    Flower,
}

public enum Suit
{
    Circle,
    Bamboo,
    Character,
}

public enum Honor
{
    East,
    South,
    West,
    North,
    RedDragon,
    GreenDragon,
    WhiteDragon,
}

public enum Flower
{
    Plum,
    Orchid,
    Chrysanthemum,
    Bamboo,
    Spring,
    Summer,
    Autumn,
    Winter,
}

[CreateAssetMenu(fileName = "NewTile", menuName = "Mahjong/Tile")]
public class TileData : ScriptableObject
{
    [Header("Identity")]
    public TileCategory category;
    public Suit suit;

    [Range(1, 9)]
    public int rank;
    public Honor honor;
    public Flower flower;

    [Header("Texture")]
    public Texture2D texture;

    public bool IsSameTile(TileData other)
    {
        if (other == null || category != other.category)
            return false;

        return category switch
        {
            TileCategory.Suit => suit == other.suit && rank == other.rank,
            TileCategory.Honor => honor == other.honor,
            _ => false,
        };
    }
}
