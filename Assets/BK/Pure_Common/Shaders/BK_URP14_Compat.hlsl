#ifndef BK_URP14_COMPAT_INCLUDED
#define BK_URP14_COMPAT_INCLUDED

// These shaders were generated against a newer URP than the project currently uses.
// Provide small compatibility shims so they can compile under URP 14 in Unity 2022.

#ifdef USE_APV_PROBE_OCCLUSION
    #undef USE_APV_PROBE_OCCLUSION
#endif

#ifdef PROBE_VOLUMES_L1
    #undef PROBE_VOLUMES_L1
#endif

#ifdef PROBE_VOLUMES_L2
    #undef PROBE_VOLUMES_L2
#endif

#if !defined(OUTPUT_SH4)
    #define OUTPUT_SH4(absolutePositionWS, normalWS, viewDir, OUT, OUT_OCCLUSION) OUTPUT_SH(normalWS, OUT)
#endif

#if UNITY_VERSION < 600022
float4 ApplyShadowClamping(float4 positionCS)
{
    #if UNITY_REVERSED_Z
        float clamped = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #else
        float clamped = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #endif

    #if _CASTING_PUNCTUAL_LIGHT_SHADOW
        return positionCS;
    #else
        positionCS.z = clamped;
    #endif

    return positionCS;
}
#endif

#endif
