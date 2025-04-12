Shader "CameraEffects/FlowerButtonDistortion"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}

		_DistortionTime ("Distortion Time", float) = 0
		_DistortionSpeed ("Distortion Speed", float) = 0.01
		_DistortionStrengthX ("Distortion Strength X", Range(0.0, 1.0)) = 0.005
		_DistortionScale ("Distortion Scale", float) = 5

		_TintTime ("Tint Time", float) = 0
		_TintSpeed ("Tint Noise Speed", float) = 0.01
		_TintScale ("Tint Noise Scale", float) = 5
		_TintMin ("Tint at Min", Color) = (1,1,1,1)
		_TintMax ("Tint at Max", Color) = (1,1,1,1)
		_TintStrength ("Tint Strength", Range(0.0, 1.0)) = 0.1

		_NoiseDebug ("Noise Debug", Range(0, 2)) = 0
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "noise.hlsl"
			
			// -=====- VERTEXT SHADER -=====-
				
			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 svPos : SV_POSITION;
			};
			
			v2f vert(float4 vertex : POSITION, float2 uv : TEXCOORD0)
			{
				v2f vout;
				vout.svPos = UnityObjectToClipPos(vertex);
				vout.uv = uv;
				return vout;
			}

			// -=====- FRAGMENT SHADER -=====-
			
			sampler2D _MainTex;
			uniform float _DistortionTime;
			uniform float _DistortionSpeed;
			uniform float _DistortionStrengthX;
			uniform float _DistortionScale;

			uniform float _TintTime;
			uniform float _TintSpeed;
			uniform float _TintScale;
			uniform fixed4 _TintMin;
			uniform fixed4 _TintMax;
			uniform float _TintStrength;

			uniform float _NoiseDebug;
			
			fixed4 frag(v2f i) : SV_Target
			{

				// Unpack and calculate global position
				float2 absPos = i.svPos.xy;
				float2 absSize = absPos / i.uv;
				float2 absCenter = absSize / 2;

				// Distortion noise
				float shift = snoise(float3(i.uv.x, i.uv.y, _DistortionTime*_DistortionSpeed)*_DistortionScale);

				if (_NoiseDebug >= 0.5 && _NoiseDebug < 1.5) {
					// Debug: show shift in color
					fixed4 debugColor = fixed4(0, 0, 0, 1);
					if (shift < 0) debugColor.r = -shift;
					else if (shift > 0) debugColor.g = shift;
					return debugColor;
				}

				// Sample with distorsion
				float2 sampleCoord = float2(i.uv.x + shift*_DistortionStrengthX, i.uv.y);

				if (sampleCoord.x < 0) sampleCoord.x = -sampleCoord.x;
				sampleCoord.x %= 2;

				fixed4 baseCol = tex2D(_MainTex, sampleCoord);

				// Tint noise
				float tintNoise = snoise(float3(i.uv.x, i.uv.y, _TintTime*_TintSpeed)*_TintScale);
				tintNoise += 1;
				tintNoise *= 0.5;
				// now tintNoise is in 0..1 range

				fixed3 tint = lerp(_TintMin, _TintMax, tintNoise).rgb;

				if (_NoiseDebug >= 1.5) {
					// Debug: show tint directly
					return fixed4(tint.rgb, 1);
				}

				// Perform tint with strength
				tint = lerp(fixed3(1,1,1), tint, _TintStrength);
				return fixed4(baseCol.rgb * tint.rgb, baseCol.a);
			}

			ENDCG
		}

	}
}
