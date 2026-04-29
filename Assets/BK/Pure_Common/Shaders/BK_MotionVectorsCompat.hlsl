#ifndef BK_MOTION_VECTORS_COMPAT_INCLUDED
#define BK_MOTION_VECTORS_COMPAT_INCLUDED

void ApplyMotionVectorZBias(inout float4 positionCS)
{
    #if defined(UNITY_REVERSED_Z)
        positionCS.z -= unity_MotionVectorsParams.z * positionCS.w;
    #else
        positionCS.z += unity_MotionVectorsParams.z * positionCS.w;
    #endif
}

float2 BK_CalcMotionVectorFromCsPositions(float4 positionCSNoJitter, float4 previousPositionCSNoJitter)
{
    float2 posNDC = positionCSNoJitter.xy * rcp(positionCSNoJitter.w);
    float2 prevPosNDC = previousPositionCSNoJitter.xy * rcp(previousPositionCSNoJitter.w);

    #if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
        UNITY_BRANCH if (_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
        {
            float2 posUV = RemapFoveatedRenderingLinearToNonUniform(posNDC * 0.5 + 0.5);
            float2 prevPosUV = RemapFoveatedRenderingPrevFrameLinearToNonUniform(prevPosNDC * 0.5 + 0.5);
            float2 velocity = posUV - prevPosUV;

            #if UNITY_UV_STARTS_AT_TOP
                velocity.y = -velocity.y;
            #endif

            return velocity;
        }
    #endif

    float2 velocity = posNDC - prevPosNDC;

    #if UNITY_UV_STARTS_AT_TOP
        velocity.y = -velocity.y;
    #endif

    return velocity * 0.5;
}

float2 CalcNdcMotionVectorFromCsPositions(float4 positionCSNoJitter, float4 previousPositionCSNoJitter)
{
    return BK_CalcMotionVectorFromCsPositions(positionCSNoJitter, previousPositionCSNoJitter);
}

float2 CalcAswNdcMotionVectorFromCsPositions(float4 positionCSNoJitter, float4 previousPositionCSNoJitter)
{
    return BK_CalcMotionVectorFromCsPositions(positionCSNoJitter, previousPositionCSNoJitter);
}

#endif
