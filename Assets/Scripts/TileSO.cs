using UnityEngine;

[CreateAssetMenu(fileName = "TileSO", menuName = "Scriptable Objects/TileSO")]
public class TileSO : ScriptableObject
{
    public TileData.TileType tileType;
    public GameObject tilePrefab;
    public string neighborString;
}
