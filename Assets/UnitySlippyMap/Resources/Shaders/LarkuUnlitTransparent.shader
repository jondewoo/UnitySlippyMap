Shader "Larku/UnlitTransparent"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB) Trans. (Alpha)", 2D) = "white" { }
    }
 
 
    SubShader
    {    
     Tags {"Queue"="Transparent" "RenderType"="Transparent" }
     Pass
     {
       ZWrite Off
       Blend SrcAlpha OneMinusSrcAlpha
 
CGPROGRAM
        #pragma vertex vert_img
        #pragma fragment frag
        #pragma fragmentoption ARB_precision_hint_fastest
        #include "UnityCG.cginc"    
 
        uniform sampler2D _MainTex;
        uniform fixed4 _Color;
 
        fixed4 frag (v2f_img i) : SV_Target
        {  
         return tex2D(_MainTex, i.uv) * _Color;  
        }
ENDCG
     }
    }
}