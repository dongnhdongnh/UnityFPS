struct appdata_color {
	float4 vertex : POSITION;
	fixed4 color : COLOR;
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
};

struct line_v2f {
	half4 col : COLOR;
	float2 normal : TEXCOORD0;
	float4 screenPos : TEXCOORD1;
	float4 originScreenPos : TEXCOORD2;
};

// d = normalized distance to line
float lineAA(float d) {
	d = max(min(d, 1.0), 0) * 1.116;
	float v = 0.93124*d*d*d - 1.42215*d*d - 0.42715*d + 0.95316;
	v /= 0.95316;
	return max(v, 0);
}

// Tranforms a direction from object to homogenous space
inline float4 UnityObjectToClipDirection(in float3 pos) {
    // More efficient than computing M*VP matrix product
    return mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(pos, 0)));
}

line_v2f line_vert (appdata_color v, float pixelWidth, float lengthPadding, out float4 outpos : SV_POSITION) {
	line_v2f o;
	float4 Mv = UnityObjectToClipPos(v.vertex);
	float4 Mn = UnityObjectToClipDirection(v.normal);

	// delta is the limit value of doing the calculation
	// x1 = M*v
	// x2 = M*(v + e*n)
    // lim e->0 (x2/x2.w - x1/x1.w)/e
    // Where M = UNITY_MATRIX_MVP, v = v.vertex, n = v.normal, e = a very small value
    // We can calculate this limit as follows
    // lim e->0 (M*(v + e*n))/(M*(v + e*n)).w - M*v/(M*v).w / e
    // lim e->0 ((M*v).w*M*(v + e*n))/((M*v).w * (M*(v + e*n)).w) - M*v*(M*(v + e*n)).w/((M*(v + e*n)).w * (M*v).w) / e
    // lim e->0 ((M*v).w*M*(v + e*n) - M*v*(M*(v + e*n)).w)/((M*v).w * (M*(v + e*n)).w) / e
    // lim e->0 ((M*v).w*M*(v + e*n) - M*v*(M*(v + e*n)).w)/((M*v).w * (M*v).w) / e
    // lim e->0 ((M*v).w*M*v + (M*v).w*e*n - M*v*(M*(v + e*n)).w)/((M*v).w * (M*v).w) / e
    // lim e->0 ((M*v).w*M*v + (M*v).w*e*n - M*v*(M*v).w - M*v*(M*e*n).w)/((M*v).w * (M*v).w) / e
    // lim e->0 ((M*v).w*M*e*n - M*v*(M*e*n).w)/((M*v).w * (M*v).w) / e
    // lim e->0 ((M*v).w*M*n - M*v*(M*n).w)/((M*v).w * (M*v).w)
    // lim e->0 M*n/(M*v).w - (M*v*(M*n).w)/((M*v).w * (M*v).w)
    
	// Previously the above calculation was done with just e = 0.001, however this could yield graphical artifacts
	// at large coordinate values as the floating point coordinates would start to run out of precision.
	// Essentially we calculate the normal of the line in screen space.
	float4 delta = (Mn - Mv*Mn.w/Mv.w) / Mv.w;
    
    // The delta (direction of the line in screen space) needs to be normalized in pixel space.
    // Otherwise it would look weird when stretched to a non-square viewport
    delta.xy *= _ScreenParams.xy;
    delta.xy = normalize(delta.xy);
    // Handle DirectX properly. See https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
	float2 normalizedScreenSpaceNormal = float2(-delta.y, delta.x) * _ProjectionParams.x;
	float2 screenSpaceNormal = normalizedScreenSpaceNormal / _ScreenParams.xy;
	float4 sn = float4(screenSpaceNormal.x, screenSpaceNormal.y, 0, 0);
	
	if (Mv.w < 0) {
		// Seems to have a very minor effect, but the distance
		// seems to be more accurate with this enabled
		sn *= -1;
	}
	
    // Left (-1) or Right (1) of the line
	float side = (v.uv.x - 0.5)*2;
    // Make the line wide
	outpos = (Mv / Mv.w) + side*sn*pixelWidth*0.5;
    
    // Start (-1) or End (1) of the line
    float start = (v.uv.y - 0.5)*2;
    // Add some additional length to the line (usually on the order of 0.5 px)
    // to avoid occational 1 pixel holes in sequences of contiguous lines.
    outpos.xy += start*(delta.xy / _ScreenParams.xy)*0.5*lengthPadding;

	// Multiply by w because homogeneous coordinates (it still needs to be clipped)
	outpos *= Mv.w;
	o.normal = normalizedScreenSpaceNormal;
	o.originScreenPos = ComputeScreenPos(Mv);
	o.screenPos = ComputeScreenPos(outpos);
	return o;
}

/** Copied from UnityCG.cginc because this function does not exist in Unity 5.2 */ 
inline bool IsGammaSpaceCompatibility() {
#if defined(UNITY_NO_LINEAR_COLORSPACE)
	return true;
#else
	// unity_ColorSpaceLuminance.w == 1 when in Linear space, otherwise == 0
	return unity_ColorSpaceLuminance.w == 0;
#endif
}