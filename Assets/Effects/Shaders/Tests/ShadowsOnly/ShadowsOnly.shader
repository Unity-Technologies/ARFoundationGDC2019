// Shader targeted for low end devices. Single Pass Forward Rendering.
Shader "Custom/Shadows Only"
{
    // Keep properties of StandardSpecular shader for upgrade reasons.
    Properties
    {

        _Alpha("Shadow Alpha", float) = 0.5
        //_Color("Color", Color) = (0.5, 0.5, 0.5, 1)
        //_MainTex("Base (RGB) Glossiness / Alpha (A)", 2D) = "white" {}

        //_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        //_Shininess("Shininess", Range(0.01, 1.0)) = 0.5
        //_GlossMapScale("Smoothness Factor", Range(0.0, 1.0)) = 1.0

        //_Glossiness("Glossiness", Range(0.0, 1.0)) = 0.5
        //[Enum(Specular Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        //[HideInInspector] _SpecSource("Specular Color Source", Float) = 0.0
        //_SpecColor("Specular", Color) = (0.5, 0.5, 0.5)
        //_SpecGlossMap("Specular", 2D) = "white" {}
        //[HideInInspector] _GlossinessSource("Glossiness Source", Float) = 0.0
        //[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        //[ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

        //[HideInInspector] _BumpScale("Scale", Float) = 1.0
        //[NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}

        //_EmissionColor("Emission Color", Color) = (0,0,0)
        //_EmissionMap("Emission", 2D) = "white" {}

        // Blending state
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0

        //[ToogleOff] _ReceiveShadows("Receive Shadows", Float) = 1.0
    }

    SubShader
    {
        //Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline" "IgnoreProjector" = "True"}
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "LightweightPipeline" "IgnoreProjector" = "True"}

        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "LightweightForward" }

            // Use same blending / depth states as Standard shader
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _ALPHAPREMULTIPLY_ON

            // -------------------------------------
            // Lightweight Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            //#pragma multi_compile_fog

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex LitPassVertexSimple
            #pragma fragment ShadowsFragmentPass
            

            #include "Packages/com.unity.render-pipelines.lightweight/Shaders/SimpleLitInput.hlsl"
            #include "ShadowsOnlyPass.hlsl"

            half4 ShadowsFragmentPass(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;
    

                half3 normalTS = SampleNormal(uv, TEXTURE2D_PARAM(_BumpMap, sampler_BumpMap));

                InputData inputData;
                InitializeInputData(input, normalTS, inputData);

                half color = MainLightRealtimeShadow(input.shadowCoord);
                
                return float4(color,color,color,1);
            };

            ENDHLSL
        }

       

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _GLOSSINESS_FROM_BASE_ALPHA

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.lightweight/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Name "Meta"
            Tags{ "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex LightweightVertexMeta
            #pragma fragment LightweightFragmentMetaSimple

            #pragma shader_feature _EMISSION
            #pragma shader_feature _SPECGLOSSMAP

            #include "Packages/com.unity.render-pipelines.lightweight/Shaders/SimpleLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.lightweight/Shaders/SimpleLitMetaPass.hlsl"

            ENDHLSL
        }
    }
    Fallback "Hidden/InternalErrorShader"
    //CustomEditor "UnityEditor.Experimental.Rendering.LightweightPipeline.SimpleLitShaderGUI"
}
