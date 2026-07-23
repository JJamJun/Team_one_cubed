Shader "UI/ShapeWipe"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _Center ("Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Scale/Radius", Range(0, 5)) = 1.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            sampler2D _MaskTex;
            
            // Unity automatically fills this with (1/width, 1/height, width, height) of the texture
            float4 _MaskTex_TexelSize; 
            
            float4 _Center;
            float _Radius;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float scale = max(_Radius, 0.0001);
                
                // 1. Automatically calculate aspect ratios
                float screenAspect = _ScreenParams.x / _ScreenParams.y;
                float texAspect = _MaskTex_TexelSize.z / _MaskTex_TexelSize.w;
                
                // 2. Shift UVs so the origin is at our target Center
                float2 maskUV = i.uv - _Center.xy;
                
                // 3. Counteract screen stretching, then counteract texture squishing
                maskUV.x *= screenAspect; 
                maskUV.x /= texAspect;    
                
                // 4. Apply the zoom/scale
                maskUV /= scale;
                
                // 5. Shift the UVs back to the 0-1 range for sampling the texture
                maskUV += float2(0.5, 0.5);

                float alpha = 1.0;

                // Only sample the texture if we are inside its bounds to avoid edge wrapping
                if (maskUV.x >= 0.0 && maskUV.x <= 1.0 && maskUV.y >= 0.0 && maskUV.y <= 1.0)
                {
                    fixed4 maskColor = tex2D(_MaskTex, maskUV);
                    alpha = 1.0 - maskColor.a; 
                }

                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}