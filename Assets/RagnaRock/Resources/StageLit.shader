Shader "RagnaRock/StageLit"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _Emission ("Emission", Range(0,3)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.25
        _Metallic ("Metallic", Range(0,1)) = 0.25
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        struct Input { float4 color : COLOR; };
        fixed4 _Color;
        half _Emission, _Smoothness, _Metallic;
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed3 c = IN.color.rgb * _Color.rgb;
            o.Albedo = c;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Emission = c * _Emission;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
