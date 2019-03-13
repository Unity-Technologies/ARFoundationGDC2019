using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class HologramBlendController : MonoBehaviour
{

    [Header("Terraformer Root References")]
    public GameObject terraformerPBRObject;
    public GameObject terraformerHologramObject;
    public Transform prefabRoot;

    [Header("Effect Editor Controller")]
    [Range(0, 1)] public float effectBlendSlider;
    private float blendValue;
    private int blendValueID;

    //Logic
    private int currentTerraformerState; //0 = PBR, 1 = Hologram

    [Header("Effect Runtime Animation Setting")]
    public AnimationCurve effectBlendCurve;
    public float sampleCurveDuration;
    private float effectBlendCurveValue;
    private bool canBlend;
    private bool currentlyBlending;

    public KeyCode editorInputKey;

    [Header("Effect Blend Shader Settings")]

    public float effectTopPosition;
    public float effectBottomPosition;

    private int arAnchorPointID;

    public Texture2D effectMap;
    private int effectMapID;

    public float effectMapTile;
    private int effectMapTileID;

    public float effectSpread;
    private int effectSpreadID;

    [ColorUsage(true, true)] public Color edgeColor;
    private int edgeColorID;

    public float edgeGap;
    private int edgeGapID;

    void OnEnable()
    {
        SetupShaderPropertyIDs(); 
    }

    void Start()
    {
        SetStartValues();
    }

    void SetStartValues()
    {
        effectBlendSlider = 0;
        currentTerraformerState = 0;
        terraformerPBRObject.SetActive(true);
        terraformerHologramObject.SetActive(false);
        currentlyBlending = false;
        canBlend = true;

        blendValue = 20f;
        Shader.SetGlobalFloat(blendValueID, blendValue);
        
    }


    void Update()
    {


        if(!Application.isPlaying)
        {
            //For testing the blend effect outside playmode

            blendValue = Mathf.Lerp(effectBottomPosition, effectTopPosition, effectBlendSlider);

            Shader.SetGlobalFloat(blendValueID, blendValue);

        } else {

            //Playmode Behaviour
/*
            if(canBlend == true)
            {
                if(currentlyBlending == false)
                {

                    
                        for (int i = 0; i < Input.touchCount; ++i)
                        {
                            if (Input.GetTouch(i).phase == TouchPhase.Began)
                            {
                                EffectBlendStart();
                            }
                        }
                

                        if(Input.GetKeyDown(editorInputKey))
                        {
                            EffectBlendStart();
                        }
                    

                }
            }
            */
        }
        
        SetGlobalShaderValues();

    }

    public void TransitionToggle()
    {
        if(canBlend == true)
        {
            if(currentlyBlending == false)
            {                    
                for (int i = 0; i < Input.touchCount; ++i)
                {
                    //if (Input.GetTouch(i).phase == TouchPhase.Began)
                    {
                        EffectBlendStart();
                    }
                }
                

                if(Input.GetKeyDown(editorInputKey))
                {
                    EffectBlendStart();
                }
                    

            }
        }
    }

    //Blend Direction
    // 0 to 1 = PBR -> Hologram
    // 1 to 0 = Hologram -> PBR
    public void EffectBlendStart()
    {
        //Setup
        currentlyBlending = true;

        terraformerPBRObject.SetActive(true);
        terraformerHologramObject.SetActive(true);
        
        switch (currentTerraformerState)
        {
            case 0:
                StartCoroutine(EffectBlendCurveSample(effectTopPosition, effectBottomPosition));

                break;

            case 1:
                StartCoroutine(EffectBlendCurveSample(effectBottomPosition, effectTopPosition));
                break;
        }

    }

    IEnumerator EffectBlendCurveSample(float bottomValue, float topValue)
    {

        float timer = 0.0f;
        
        while(timer <= sampleCurveDuration)
        {
            
            blendValue = Mathf.Lerp(bottomValue, topValue, effectBlendCurve.Evaluate(timer/sampleCurveDuration));

            timer += Time.deltaTime;

            Shader.SetGlobalFloat(blendValueID, blendValue);

            if(timer >= sampleCurveDuration)
            {

                EffectBlendFinished();
                
            }

            yield return null;


        }
    }

    public void EffectBlendFinished()
    {

        currentTerraformerState += 1;
                
        if(currentTerraformerState > 1){
            currentTerraformerState = 0;
        }

       if(currentTerraformerState == 0)
       {
           terraformerHologramObject.SetActive(false);

       } else if(currentTerraformerState ==  1)
       {
           terraformerPBRObject.SetActive(false);
       }

        currentlyBlending = false;

    }

    void SetupShaderPropertyIDs()
    {
        arAnchorPointID = Shader.PropertyToID("_AR_AnchorPoint");
        blendValueID = Shader.PropertyToID("_Effect_Blend");
        effectMapID = Shader.PropertyToID("_Effect_Map");
        effectMapTileID = Shader.PropertyToID("_Effect_MapTile");
        effectSpreadID = Shader.PropertyToID("_Effect_Spread");
        edgeColorID = Shader.PropertyToID("_Edge_Color");
        edgeGapID = Shader.PropertyToID("_Edge_Gap");
    }

    void SetGlobalShaderValues()
    {
        Shader.SetGlobalVector(arAnchorPointID, prefabRoot.position);
        Shader.SetGlobalTexture(effectMapID, effectMap);
        Shader.SetGlobalFloat(effectMapTileID, effectMapTile);
        Shader.SetGlobalFloat(effectSpreadID, effectSpread);
        Shader.SetGlobalColor(edgeColorID, edgeColor); 
        Shader.SetGlobalFloat(edgeGapID, edgeGap);
    }

}
