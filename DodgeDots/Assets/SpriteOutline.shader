Shader "Custom/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,0.5,0,1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 2.0
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineWidth;

            v2f vert(appdata_t IN) {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // 采样周围像素检测透明度边界
                float2 unit = _MainTex_TexelSize.xy * _OutlineWidth;
                fixed4 alpha = fixed4(0,0,0,0);
                alpha += tex2D(_MainTex, IN.texcoord + float2(unit.x, 0));
                alpha += tex2D(_MainTex, IN.texcoord - float2(unit.x, 0));
                alpha += tex2D(_MainTex, IN.texcoord + float2(0, unit.y));
                alpha += tex2D(_MainTex, IN.texcoord - float2(0, unit.y));

                if (c.a < 0.1 && alpha.a > 0.1) {
                    return _OutlineColor;
                }

                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
