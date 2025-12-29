using UnityEngine;

public struct TileData
{
    public enum TileType
    {
        Water = 0, 
        Coast = 1,
        Land = 2
    }
    public TileType tileType;
    public int id;
    public int zone;
    public bool isStartPosition;
}
