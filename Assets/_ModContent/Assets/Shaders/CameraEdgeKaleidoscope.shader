Shader "CameraEffects/CameraEdgeKaleidoscope"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_SegmentCount ("Segment count", Float) = 8
		_SegmentCloseness ("Segment closeness", Range(0.0, 1.0)) = 0.2
		_SegmentUniformClosenessThreshold ("Segment uniform closeness threshold", Range(0.0, 1.0)) = 0.6
		_SegmentPhaseRadians ("Segment phase radians", Float) = 0
		_Tint ("Tint", Color) = (1,1,1,1)
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

			#define PI 3.1415926538

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
			uniform float _SegmentCount;
			uniform float _SegmentCloseness;
			uniform float _SegmentUniformClosenessThreshold;
			uniform float _SegmentPhaseRadians;
			
			fixed4 frag(v2f i) : SV_Target
			{

				// Unpack and calculate global position
				float2 globalPos = i.svPos.xy;
				float2 globalSize = globalPos / i.uv;
				float2 globalCenter = globalSize / 2;

				// Find base color
				fixed4 baseCol = tex2D(_MainTex, i.uv);

				// Add segments
				for (float segI = 0; segI < _SegmentCount; segI+=1) {

					// Angle
					float angleRadians = segI*(2*PI/_SegmentCount) + _SegmentPhaseRadians;
					float halfAbsSinAngle = 0.5*abs(sin(angleRadians));
					float halfAbsCosAngle = 0.5*abs(cos(angleRadians));
					
					// Length and closeness from edge intesection to center
					float unitLengthEdgeToCenter = halfAbsSinAngle + halfAbsCosAngle;
					float absoluteLengthEdgeToCenter = globalSize.y*halfAbsSinAngle + globalSize.x*halfAbsCosAngle;
					float closenessToCenter;

					if (_SegmentCloseness <= _SegmentUniformClosenessThreshold) {
						float absoluteClosenessToCenter = _SegmentCloseness * globalSize.y*0.5;
						closenessToCenter = absoluteClosenessToCenter/absoluteLengthEdgeToCenter;
					} else {
						float maxAbsoluteClosenessToCenter = _SegmentUniformClosenessThreshold * globalSize.y*0.5;
						closenessToCenter = maxAbsoluteClosenessToCenter/absoluteLengthEdgeToCenter;

						float addentProgress = (_SegmentCloseness - _SegmentUniformClosenessThreshold) / (1 - _SegmentUniformClosenessThreshold);
						closenessToCenter = lerp(closenessToCenter, 1, addentProgress);
					}										

					// Segment range:
					// (x-0.5)cos(a) + (y-0.5)sin(a) < lerp(-l, 0, q)
					// q := closenessToCenter (0..1)
					// a := angleRadians
					// l := unitLengthEdgeToCenter
					bool isInRange = (i.uv.x - 0.5)*cos(angleRadians) + (i.uv.y - 0.5)*sin(angleRadians) < lerp(-unitLengthEdgeToCenter, 0, closenessToCenter);
					
					if (isInRange) {
						
						// Global pos is end position
						// end position needs to be "untransformed" back
						// to the og texture position
						float2 transformedGlobalPos = globalPos;

						// Rotate the block back to be at angle 0
						// Angle 0 points right (+x)
						float radius = length(transformedGlobalPos - globalCenter);
						
						float curAngleRadians = acos((transformedGlobalPos.x-globalCenter.x)/radius);
						if (i.uv.y < 0.5) { curAngleRadians = -curAngleRadians; };
						curAngleRadians -= angleRadians; //angleRadians is segment's angle

						transformedGlobalPos = float2(
							globalCenter.x+radius*cos(curAngleRadians),
							globalCenter.y+radius*sin(curAngleRadians)
						);

						// Move the point so that relative centes match
						transformedGlobalPos.x += (1-closenessToCenter)*absoluteLengthEdgeToCenter;

						// Rotate texture around itself so that 
						// the top faces the cetnter at angle 0
						radius = length(transformedGlobalPos - globalCenter);
						
						curAngleRadians = acos((transformedGlobalPos.x-globalCenter.x)/radius);
						if (transformedGlobalPos.y < globalCenter.y) { curAngleRadians = -curAngleRadians; };
						curAngleRadians += PI/2;

						transformedGlobalPos = float2(
							globalCenter.x+radius*cos(curAngleRadians),
							globalCenter.y+radius*sin(curAngleRadians)
						);

						// Sample						
						fixed4 doubleVisionCol = tex2D(_MainTex, transformedGlobalPos/globalSize);
						doubleVisionCol *= 0.5;
						baseCol = saturate(doubleVisionCol+baseCol);
						
					}

				}

				return baseCol;
			}
			
			ENDCG
		}

	}
}
