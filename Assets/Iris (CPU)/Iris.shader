Shader "Unlit/Iris"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _Lines1 ("Texture", 2D) = "white" {}
                _Structure ("_Structure", 2D) = "white" {}
                                _iris ("_iris", 2D) = "white" {}

                        _Distortion ("_Distortion", float) = 0
                                                _irisStructure ("_irisStructure", Range (0, 1)) = 1

	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

            sampler2D _Lines1;
            sampler2D _Structure;
            sampler2D _iris;

            float _irisStructure;

            float _Distortion;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture

                float2 scaledUV=(i.uv-0.5)*((sin(_Time.y)*0.5+0.5)*0.3+0.7)+0.5;

                fixed4 innerLines = tex2D(_Lines1, i.uv);
                fixed4 structure = tex2D(_Structure,scaledUV);
            

                float ir=tex2D(_iris, scaledUV)*0.3+0.7;

            
                float2 dir= normalize(float2(0.5,0.5)- i.uv)*0.1;

                float l= length(float2(0.5,0.5)- i.uv)*2;

				fixed4 col = tex2D(_MainTex, i.uv-dir*innerLines.r*innerLines.a*_Distortion+dir*(pow(structure.g,3)+(structure.r*2-1)*0.1)*0.5f+pow(ir,4)*1.0*dir*_irisStructure)-innerLines.r*innerLines.a*0.3f;
                col.rgb-= (pow(structure.g,3)+(structure.r*2-1)*0.1)*0.7f*float3(1,1,0);
                
                col.r= clamp(col.r-((structure.b)*0.1)*clamp(0.2-col.r,0,1),0,1)*1.2;

                 col.rgb=lerp(col.rgb*float3(ir,ir,1),col.rgb*float3(ir,ir,ir) ,l);
          
				// apply fog
				return clamp(col,0,1);
			}
			ENDCG
		}
	}
}
