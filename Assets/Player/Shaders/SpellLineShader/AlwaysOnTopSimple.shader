Shader "Custom/AlwaysOnTop"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)  // Domyślny kolor (biały)
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        ZTest Always
        ZWrite Off
        Blend One OneMinusSrcAlpha  // Mieszanie Alpha Blend Premultiply

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;  // Odczytaj kolor z LineRenderer
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;  // Przekaż kolor do fragment shadera
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Premultiply color by alpha
                fixed4 color = i.color;
                color.rgb *= color.a;  // Premultiply RGB by Alpha
                return color;
            }
            ENDCG
        }
    }
}
