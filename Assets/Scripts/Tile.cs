using System;
using UnityEditor;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public TileData tileData;
    
    private MaterialPropertyBlock mpb;
    
    [SerializeField] private Renderer tileMR;
    
    private static readonly int TopRight = Shader.PropertyToID("_TopRight");
    private static readonly int Right = Shader.PropertyToID("_Right");
    private static readonly int BottomRight = Shader.PropertyToID("_BottomRight");
    private static readonly int TopLeft = Shader.PropertyToID("_TopLeft");
    private static readonly int Left = Shader.PropertyToID("_Left");
    private static readonly int BottomLeft = Shader.PropertyToID("_BottomLeft");
    private static readonly int BorderAnimationSpeed = Shader.PropertyToID("BorderAnimationSpeed");
    private static readonly int DashFrequency = Shader.PropertyToID("DashFrequency");

    public void Initialize(TileData tileData)
    {
        this.tileData = tileData;
        if (tileMR == null) tileMR = GetComponentInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    public void SetBorders(bool[] borders, bool animated, bool dashed)
    {
        mpb.SetFloat(TopRight, borders[0] ? 1f : 0f);
        mpb.SetFloat(Right, borders[1] ? 1f : 0f);
        mpb.SetFloat(BottomRight, borders[2] ? 1f : 0f);
        mpb.SetFloat(BottomLeft, borders[3] ? 1f : 0f);
        mpb.SetFloat(Left, borders[4] ? 1f : 0f);
        mpb.SetFloat(TopLeft, borders[5] ? 1f : 0f);
        mpb.SetFloat(BorderAnimationSpeed, animated ? 0.2f : 0f);
        mpb.SetFloat(DashFrequency, dashed ? 5f : 0f);
        tileMR.SetPropertyBlock(mpb);
    }

    private void OnDrawGizmos()
    {
        var style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.gray7;
        Handles.Label(transform.position, $"{tileData.id}\n{(int)tileData.tileType}", style);

        if (tileData.isStartPosition)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one);
        }
    }
}
