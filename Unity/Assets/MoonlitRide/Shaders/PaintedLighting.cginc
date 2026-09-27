#ifndef MOONLIT_PAINTED_LIGHTING
#define MOONLIT_PAINTED_LIGHTING
// Three broad painted tones. Keep attenuation continuous so lantern fades,
// window cookies and soft shadow maps retain their existing behaviour.
half4 LightingPainted(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
{
    half n = saturate(dot(s.Normal, lightDir));
    half band = .10 + .40 * smoothstep(.12, .22, n) + .50 * smoothstep(.52, .62, n);
    half3 tint = lerp(half3(.72,.80,1.0), half3(1.0,.95,.82), band);
    half3 h = normalize(lightDir + viewDir);
    half highlight = smoothstep(.965,.985,saturate(dot(s.Normal,h))) * s.Gloss * .12;
    return half4((s.Albedo * tint * band + highlight) * _LightColor0.rgb * atten, s.Alpha);
}
#endif
