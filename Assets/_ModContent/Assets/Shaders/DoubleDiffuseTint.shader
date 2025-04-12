// Based on KT/Mobile/DiffuseTint

Shader "KT/Mobile/DoubleDiffuseTint" {
	Properties {
		_MainTex("Main Tex (Blend 0) (RGB)", 2D) = "white" {}
		_MainColor("Main Tint", Color) = (1,1,1,1)
		_SecondTex("Second Tex (Blend 1) (RGBA)", 2D) = "white" {}
		_SecondColor("Second Tint", Color) = (1,1,1,1)
	}
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 150

		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		sampler2D _SecondTex;
		fixed4 _MainColor;
		fixed4 _SecondColor;

		struct Input {
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutput o) {
			fixed4 c1 = tex2D(_MainTex, IN.uv_MainTex) * _MainColor;
			fixed4 c2 = tex2D(_SecondTex, IN.uv_MainTex) * _SecondColor;
			fixed4 c = c1 * (1 - c2.a) + c2 * c2.a;
			o.Albedo = c.rgb;
			o.Alpha = 1;
		}

		ENDCG
	}

	Fallback "Mobile/Diffuse"
}
