Shader "Custom/WireframeTerrainShader" {
	// Based on https://github.com/Chaser324/unity-wireframe
	// Minor modifications made to use vertex colours
	Properties {
        _WireThickness ("Wire Thickness", RANGE(0, 800)) = 100
        _WireColor ("Wire Color", Color) = (0.0, 0.0, 0.0, 1.0)
	}
	SubShader {
		Tags { "RenderType"="Opaque" "LightMode"="ForwardBase" }
		
		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"
			#include "Lighting.cginc"

            // compile shader into multiple variants, with and without shadows
            // (we don't care about any lightmaps yet, so skip these variants)
            //#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            // shadow helper functions and macros
            #include "AutoLight.cginc"

			float _WireThickness;
			uniform float4 _WireColor; 

			struct appdata
			{
				float4 _ShadowCoord : TEXCOORD0;
				float4 vertex : POSITION;
				float4 normal : NORMAL;
				float4 color : COLOR0;
			};

			struct v2g
			{
				float4 _ShadowCoord : TEXCOORD0;
				float4 projectionSpaceVertex : SV_POSITION;
				float4 worldSpacePosition : TEXCOORD1;
				float4 color : COLOR0;
				fixed4 diff : COLOR1;
				fixed4 ambient : COLOR2;
			};

			struct g2f
			{
				float4 _ShadowCoord : TEXCOORD0;
				float4 projectionSpaceVertex : SV_POSITION;
				float4 worldSpacePosition : TEXCOORD1;
				float4 dist : TEXCOORD2;
				float4 color : COLOR0;
				fixed4 diff : COLOR1;
				fixed4 ambient : COLOR2;
			};
			
			v2g vert (appdata v)
			{
				v2g o;
				UNITY_INITIALIZE_OUTPUT(v2g,o);
				o.projectionSpaceVertex = UnityObjectToClipPos(v.vertex);
				o.worldSpacePosition = mul(unity_ObjectToWorld, v.vertex);
				o.color = v.color;

                // get vertex normal in world space
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                // dot product between normal and light direction for
                // standard diffuse (Lambert) lighting
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                // factor in the light color
                o.diff = nl * _LightColor0;

                // the only difference from previous shader:
                // in addition to the diffuse lighting from the main light,
                // add illumination from ambient or light probes
                // ShadeSH9 function from UnityCG.cginc evaluates it,
                // using world space normal
                o.ambient.rgb = ShadeSH9(half4(worldNormal,1));

                // compute shadows data
                o._ShadowCoord = ComputeScreenPos(o.projectionSpaceVertex);

				return o;
			}
			
			[maxvertexcount(3)]
			void geom(triangle v2g i[3], inout TriangleStream<g2f> triangleStream)
			{
				float2 p0 = i[0].projectionSpaceVertex.xy / i[0].projectionSpaceVertex.w;
				float2 p1 = i[1].projectionSpaceVertex.xy / i[1].projectionSpaceVertex.w;
				float2 p2 = i[2].projectionSpaceVertex.xy / i[2].projectionSpaceVertex.w;

				float2 edge0 = p2 - p1;
				float2 edge1 = p2 - p0;
				float2 edge2 = p1 - p0;

				// To find the distance to the opposite edge, we take the
				// formula for finding the area of a triangle Area = Base/2 * Height, 
				// and solve for the Height = (Area * 2)/Base.
				// We can get the area of a triangle by taking its cross product
				// divided by 2.  However we can avoid dividing our area/base by 2
				// since our cross product will already be double our area.
				float area = abs(edge1.x * edge2.y - edge1.y * edge2.x);
				float wireThickness = 800 - _WireThickness;

				g2f o;

				o.worldSpacePosition = i[0].worldSpacePosition;
				o.projectionSpaceVertex = i[0].projectionSpaceVertex;
				o.dist.xyz = float3( (area / length(edge0)), 0.0, 0.0) * o.projectionSpaceVertex.w * wireThickness;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				o.color = i[0].color;
				o.diff = i[0].diff;
				o.ambient = i[0].ambient;
				o._ShadowCoord = i[0]._ShadowCoord;
				triangleStream.Append(o);

				o.worldSpacePosition = i[1].worldSpacePosition;
				o.projectionSpaceVertex = i[1].projectionSpaceVertex;
				o.dist.xyz = float3(0.0, (area / length(edge1)), 0.0) * o.projectionSpaceVertex.w * wireThickness;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				o.color = i[1].color;
 				o.diff = i[1].diff;
				o.ambient = i[1].ambient;
				o._ShadowCoord = i[1]._ShadowCoord;
				triangleStream.Append(o);

				o.worldSpacePosition = i[2].worldSpacePosition;
				o.projectionSpaceVertex = i[2].projectionSpaceVertex;
				o.dist.xyz = float3(0.0, 0.0, (area / length(edge2))) * o.projectionSpaceVertex.w * wireThickness;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				o.color = i[2].color;
				o.diff = i[2].diff;
				o.ambient = i[2].ambient;
				o._ShadowCoord = i[2]._ShadowCoord;
				triangleStream.Append(o);
			}

			fixed4 frag (g2f i) : SV_Target
			{
				float minDistanceToEdge = min(i.dist[0], min(i.dist[1], i.dist[2])) * i.dist[3];

                // compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
                fixed shadow = SHADOW_ATTENUATION(i);

				// Early out if we know we are not on a line segment.
				if(minDistanceToEdge > 0.9)
				{
					return fixed4(i.color.rgb * (i.diff * shadow + i.ambient),0);
				}

				// Smooth our line out
				float t = exp2(-2 * minDistanceToEdge * minDistanceToEdge);
				fixed4 finalColor = lerp(i.color, _WireColor, t * 0.1f);
				finalColor.a = t;

				return finalColor * (i.diff * shadow + i.ambient);
			} 

			ENDCG
		}


	}
	FallBack "Diffuse"
}
