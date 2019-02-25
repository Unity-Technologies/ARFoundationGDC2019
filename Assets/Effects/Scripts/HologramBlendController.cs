using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class HologramBlendController : MonoBehaviour
{
    public float blendValue;
    private int blendValueID;

    public Texture2D effectMap;
    private int effectMapID;

    public float effectMapTile;
    private int effectMapTileID;

    public float effectSpread;
    private int effectSpreadID;

    [ColorUsage(true, true)] public Color edgeColor;
    private int edgeColorID;


    void OnEnable()
    {
        blendValueID = Shader.PropertyToID("_Effect_Blend");
        effectMapID = Shader.PropertyToID("_Effect_Map");
        effectMapTileID = Shader.PropertyToID("_Effect_MapTile");
        effectSpreadID = Shader.PropertyToID("_Effect_Spread");
        edgeColorID = Shader.PropertyToID("_Edge_Color");
    }


    void Update()
    {
       Shader.SetGlobalFloat(blendValueID, blendValue);
       Shader.SetGlobalTexture(effectMapID, effectMap);
       Shader.SetGlobalFloat(effectMapTileID, effectMapTile);
       Shader.SetGlobalFloat(effectSpreadID, effectSpread);
       Shader.SetGlobalColor(edgeColorID, edgeColor);
    }

}
