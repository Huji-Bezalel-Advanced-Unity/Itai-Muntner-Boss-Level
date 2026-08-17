// One shader serving every sprite effect in the fight: the white-out on a hit, the coloured
// tell during a wind-up, the phase tint, and the dissolve on death.
//
// They are separate properties rather than separate shaders on purpose. Before this existed,
// the damage flash and the attack telegraph both wrote SpriteRenderer.color and overwrote each
// other, so a hit landing during a wind-up simply did not read. Here they compose: the flash
// applies on top of whatever tint is already running.
//
// Written by hand rather than in Shader Graph because a .shadergraph file is generated JSON,
// which is neither readable nor reviewable — and the whole effect fits in one page of HLSL.
Shader "Boss Level/Sprite Effects"
{
    Properties
    {
        // PerRendererData because a SpriteRenderer supplies the texture itself, per sprite,
        // rather than it being an authored property of the material.
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color ("Material Tint", Color) = (1, 1, 1, 1)

        [Header(Damage Flash)]
        _FlashColour ("Flash Colour", Color) = (1, 1, 1, 1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0

        [Header(Telegraph Tint)]
        _TintColour ("Tint Colour", Color) = (1, 1, 1, 1)
        _TintAmount ("Tint Amount", Range(0, 1)) = 0

        [Header(Phase)]
        _PhaseTint ("Phase Tint", Color) = (1, 1, 1, 1)

        [Header(Dissolve)]
        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveScale ("Dissolve Noise Scale", Range(1, 64)) = 18
        _DissolveEdge ("Dissolve Edge Width", Range(0.001, 0.5)) = 0.08
        _DissolveEdgeColour ("Dissolve Edge Colour", Color) = (1, 0.6, 0.15, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 colour     : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 colour      : COLOR;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _FlashColour;
                float  _FlashAmount;
                float4 _TintColour;
                float  _TintAmount;
                float4 _PhaseTint;
                float  _DissolveAmount;
                float  _DissolveScale;
                float  _DissolveEdge;
                float4 _DissolveEdgeColour;
            CBUFFER_END

            // Value noise, generated rather than sampled from a texture so the effect needs no
            // authored asset and cannot be broken by one going missing.
            float Hash(float2 position)
            {
                return frac(sin(dot(position, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 position)
            {
                float2 cell = floor(position);
                float2 offset = frac(position);

                // Smoothstep the interpolation so cells blend instead of showing their edges.
                offset = offset * offset * (3.0 - 2.0 * offset);

                float bottomLeft  = Hash(cell);
                float bottomRight = Hash(cell + float2(1, 0));
                float topLeft     = Hash(cell + float2(0, 1));
                float topRight    = Hash(cell + float2(1, 1));

                return lerp(
                    lerp(bottomLeft, bottomRight, offset.x),
                    lerp(topLeft, topRight, offset.x),
                    offset.y);
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;

                output.positionHCS = TransformObjectToHClip(input.positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.colour = input.colour;

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 colour = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv)
                             * input.colour
                             * _Color;

                // Phase tint multiplies, so it recolours the sprite without brightening it.
                colour.rgb *= _PhaseTint.rgb;

                // The wind-up tell blends towards a hue.
                colour.rgb = lerp(colour.rgb, _TintColour.rgb, _TintAmount);

                // The damage flash goes last, so a hit still reads clearly during a wind-up.
                // This ordering is the entire reason these are separate properties.
                colour.rgb = lerp(colour.rgb, _FlashColour.rgb, _FlashAmount);

                float noise = ValueNoise(input.uv * _DissolveScale);

                // Scaled slightly past 1 so a full dissolve clears every last pixel.
                float remaining = noise - _DissolveAmount * (1.0 + _DissolveEdge);
                clip(remaining);

                // Glow along whatever is about to disappear next, which is what makes a dissolve
                // read as burning away rather than as fading out.
                float edge = 1.0 - saturate(remaining / _DissolveEdge);
                float dissolving = step(0.001, _DissolveAmount);
                colour.rgb = lerp(colour.rgb, _DissolveEdgeColour.rgb, edge * dissolving);

                return colour;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
