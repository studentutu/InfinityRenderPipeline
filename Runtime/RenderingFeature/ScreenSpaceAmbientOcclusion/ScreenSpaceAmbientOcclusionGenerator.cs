using System;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace InfinityTech.Rendering.Feature
{
    public struct GrountTruthAmbientOcclusionParameter
    {
        public int numRay;
        public int numStep;
        public float power;
        public float radius;
        public float intensity;
        public float sharpeness;
        public float temporalScale;
        public float temporalWeight;
    }

    public struct GrountTruthAmbientOcclusionInputData
    {
        public float halfProjScale;
        public float temporalOffset;
        public float temporalDirection;
        public float4 resolution;
        public float4 upsampleSize;
        public float4x4 matrix_Proj;
        public float4x4 matrix_InvProj;
        public float4x4 matrix_ViewProj;
        public float4x4 matrix_InvViewProj;
        public float4x4 matrix_ViewToWorld;
        public float4x4 matrix_WorldToView;
        public RenderTargetIdentifier depthTexture;
        public RenderTargetIdentifier normalTexture;
        public RenderTargetIdentifier motionTexture;
    }

    public struct GrountTruthAmbientOcclusionOutputData
    {
        public RenderTargetIdentifier occlusionTexture;
        public RenderTargetIdentifier spatialTexture;
        public RenderTargetIdentifier upsampleTexture;
        public RenderTargetIdentifier accmulateTexture;
        public RenderTargetIdentifier historyTexture;
    }

    internal struct GrountTruthAmbientOcclusionUnifrom
    {
        public int numRay; 
        public int numStep;
        public float power; 
        public float radius; 
        public float intensity; 
        public float sharpeness;
        public float halfProjScale; 
        public float temporalOffset; 
        public float temporalDirection;
        public float temporalScale;
        public float temporalWeight;
        public float pending2;
        public float4 resolution;
        public float4 upsampleSize;
        public float4x4 matrix_Proj; 
        public float4x4 matrix_InvProj; 
        public float4x4 matrix_ViewProj; 
        public float4x4 matrix_InvViewProj; 
        public float4x4 matrix_ViewToWorld; 
        public float4x4 matrix_WorldToView;
    }

    internal static class GrountTruthAmbientOcclusionShaderID
    {
        public static int UnifromData = Shader.PropertyToID("CBV_OcclusionUnifrom");
        public static int DepthTexture = Shader.PropertyToID("SRV_DepthTexture");
        public static int NormalTexture = Shader.PropertyToID("SRV_NormalTexture");
        public static int MotionTexture = Shader.PropertyToID("SRV_MotionTexture");
        public static int OcclusionTexture = Shader.PropertyToID("UAV_OcclusionTexture");
        public static int OcclusionTextureRead = Shader.PropertyToID("SRV_OcclusionTexture");
        public static int SpatialTexture = Shader.PropertyToID("UAV_SpatialTexture");
        public static int HistoryTexture = Shader.PropertyToID("SRV_HistoryTexture");
        public static int AccmulateTexture = Shader.PropertyToID("UAV_AccmulateTexture");
        public static int UpsampleTexture = Shader.PropertyToID("UAV_UpsampleTexture");
    }

    public sealed class GrountTruthAmbientOcclusionGenerator
    {
        private int m_UnifromStride;
        private ComputeShader m_Shader;
        private ComputeBuffer m_OcclusionUnifrom;
        private GrountTruthAmbientOcclusionUnifrom[] m_UnifromData;
        private static readonly float[] s_SpatialOffsets = {0, 0.5f, 0.25f, 0.75f};
	    private static readonly float[] s_TemporalRotations = {60, 300, 180, 240, 120, 0};

        public GrountTruthAmbientOcclusionGenerator(ComputeShader shader)
        {
            m_Shader = shader;
            m_UnifromData = new GrountTruthAmbientOcclusionUnifrom[1];
            m_UnifromStride = Marshal.SizeOf(typeof(GrountTruthAmbientOcclusionUnifrom));
            m_OcclusionUnifrom = new ComputeBuffer(1, m_UnifromStride);
        }

        public void Release()
        {
            m_OcclusionUnifrom.Dispose();
        }

        public void GetJitterInfo(in int frameIndex, ref float temporalOffset, ref float temporalRotation)
        {
            temporalRotation = s_TemporalRotations[frameIndex % 6];
            temporalRotation /= 360;
            temporalOffset = s_SpatialOffsets[(frameIndex / 6) % 4];
        }

        private void SetUnifrom(in GrountTruthAmbientOcclusionParameter parameter, in GrountTruthAmbientOcclusionInputData inputData) 
        {
            m_UnifromData[0].numRay = parameter.numRay; 
            m_UnifromData[0].numStep = parameter.numStep;
            m_UnifromData[0].power = parameter.power; 
            m_UnifromData[0].radius = parameter.radius; 
            m_UnifromData[0].intensity = parameter.intensity; 
            m_UnifromData[0].sharpeness = parameter.sharpeness;
            m_UnifromData[0].halfProjScale = inputData.halfProjScale; 
            m_UnifromData[0].temporalOffset = inputData.temporalOffset; 
            m_UnifromData[0].temporalDirection = inputData.temporalDirection;
            m_UnifromData[0].temporalScale = parameter.temporalScale;
            m_UnifromData[0].temporalWeight = parameter.temporalWeight;
            m_UnifromData[0].pending2 = 0;
            m_UnifromData[0].resolution = inputData.resolution;
            m_UnifromData[0].upsampleSize = inputData.upsampleSize;
            m_UnifromData[0].matrix_Proj = inputData.matrix_Proj; 
            m_UnifromData[0].matrix_InvProj = inputData.matrix_InvProj; 
            m_UnifromData[0].matrix_ViewProj = inputData.matrix_ViewProj; 
            m_UnifromData[0].matrix_InvViewProj = inputData.matrix_InvViewProj; 
            m_UnifromData[0].matrix_ViewToWorld = inputData.matrix_ViewToWorld; 
            m_UnifromData[0].matrix_WorldToView = inputData.matrix_WorldToView;
            m_OcclusionUnifrom.SetData(m_UnifromData);
        }

        public void Render(CommandBuffer cmdBuffer, in GrountTruthAmbientOcclusionParameter parameter, in GrountTruthAmbientOcclusionInputData inputData, in GrountTruthAmbientOcclusionOutputData outoutData) 
        {
            SetUnifrom(parameter, inputData);
            cmdBuffer.SetComputeConstantBufferParam(m_Shader, GrountTruthAmbientOcclusionShaderID.UnifromData, m_OcclusionUnifrom, 0, m_UnifromStride);

            cmdBuffer.BeginSample("RayMarch");
            cmdBuffer.SetComputeTextureParam(m_Shader, 0, GrountTruthAmbientOcclusionShaderID.DepthTexture, inputData.depthTexture);
            cmdBuffer.SetComputeTextureParam(m_Shader, 0, GrountTruthAmbientOcclusionShaderID.NormalTexture, inputData.normalTexture);
            cmdBuffer.SetComputeTextureParam(m_Shader, 0, GrountTruthAmbientOcclusionShaderID.OcclusionTexture, outoutData.occlusionTexture);
            cmdBuffer.DispatchCompute(m_Shader, 0,  Mathf.CeilToInt(inputData.resolution.x / 16),  Mathf.CeilToInt(inputData.resolution.y / 16), 1);
            cmdBuffer.EndSample("RayMarch");

            cmdBuffer.BeginSample("SpatialX");
            cmdBuffer.SetComputeTextureParam(m_Shader, 1, GrountTruthAmbientOcclusionShaderID.DepthTexture, inputData.depthTexture);
            cmdBuffer.SetComputeTextureParam(m_Shader, 1, GrountTruthAmbientOcclusionShaderID.SpatialTexture, outoutData.spatialTexture);
            cmdBuffer.SetComputeTextureParam(m_Shader, 1, GrountTruthAmbientOcclusionShaderID.OcclusionTextureRead, outoutData.occlusionTexture);
            cmdBuffer.DispatchCompute(m_Shader, 1, Mathf.CeilToInt(inputData.resolution.x / 16), Mathf.CeilToInt(inputData.resolution.y / 16), 1);
            cmdBuffer.EndSample("SpatialX");

            cmdBuffer.BeginSample("SpatialY");
            cmdBuffer.SetComputeTextureParam(m_Shader, 2, GrountTruthAmbientOcclusionShaderID.DepthTexture, inputData.depthTexture);
            cmdBuffer.SetComputeTextureParam(m_Shader, 2, GrountTruthAmbientOcclusionShaderID.SpatialTexture, outoutData.occlusionTexture);
            cmdBuffer.SetComputeTextureParam(m_Shader, 2, GrountTruthAmbientOcclusionShaderID.OcclusionTextureRead, outoutData.spatialTexture);
            cmdBuffer.DispatchCompute(m_Shader, 2, Mathf.CeilToInt(inputData.resolution.x / 16), Mathf.CeilToInt(inputData.resolution.y / 16), 1);
            cmdBuffer.EndSample("SpatialY");

            cmdBuffer.BeginSample("Temporal");
            cmdBuffer.SetComputeTextureParam(m_Shader, 3, GrountTruthAmbientOcclusionShaderID.MotionTexture, inputData.motionTexture);
            cmdBuffer.SetComputeTextureParam(m_Shader, 3, GrountTruthAmbientOcclusionShaderID.AccmulateTexture, outoutData.accmulateTexture);
            cmdBuffer.SetComputeTextureParam(m_Shader, 3, GrountTruthAmbientOcclusionShaderID.OcclusionTextureRead, outoutData.occlusionTexture);
            cmdBuffer.SetComputeTextureParam(m_Shader, 3, GrountTruthAmbientOcclusionShaderID.HistoryTexture, outoutData.historyTexture);
            cmdBuffer.DispatchCompute(m_Shader, 3, Mathf.CeilToInt(inputData.upsampleSize.x / 16), Mathf.CeilToInt(inputData.upsampleSize.y / 16), 1);
            cmdBuffer.CopyTexture(outoutData.accmulateTexture, outoutData.historyTexture);
            cmdBuffer.EndSample("Temporal");

            /*cmdBuffer.BeginSample("Upsample");
            cmdBuffer.SetComputeTextureParam(m_Shader, 3, GrountTruthAmbientOcclusionShaderID.DepthTexture, inputData.depthTexture);
            cmdBuffer.SetComputeTextureParam(m_Shader, 3, GrountTruthAmbientOcclusionShaderID.NormalTexture, inputData.normalTexture);
            cmdBuffer.SetComputeTextureParam(m_Shader, 3, GrountTruthAmbientOcclusionShaderID.OcclusionTextureRead, outoutData.occlusionTexture);
            cmdBuffer.SetComputeTextureParam(m_Shader, 3, GrountTruthAmbientOcclusionShaderID.UpsampleTexture, outoutData.upsampleTexture);
            cmdBuffer.DispatchCompute(m_Shader, 3, Mathf.CeilToInt(inputData.upsampleSize.x / 16), Mathf.CeilToInt(inputData.upsampleSize.y / 16), 1);
            cmdBuffer.EndSample("Upsample");

            cmdBuffer.BeginSample("Temporal");
            cmdBuffer.SetComputeTextureParam(m_Shader, 4, GrountTruthAmbientOcclusionShaderID.MotionTexture, inputData.motionTexture);
            cmdBuffer.SetComputeTextureParam(m_Shader, 4, GrountTruthAmbientOcclusionShaderID.AccmulateTexture, outoutData.accmulateTexture);
            cmdBuffer.SetComputeTextureParam(m_Shader, 4, GrountTruthAmbientOcclusionShaderID.OcclusionTextureRead, outoutData.upsampleTexture);
            cmdBuffer.SetComputeTextureParam(m_Shader, 4, GrountTruthAmbientOcclusionShaderID.HistoryTexture, outoutData.historyTexture);
            cmdBuffer.DispatchCompute(m_Shader, 4, Mathf.CeilToInt(inputData.upsampleSize.x / 16), Mathf.CeilToInt(inputData.upsampleSize.y / 16), 1);
            cmdBuffer.CopyTexture(outoutData.accmulateTexture, outoutData.historyTexture);
            cmdBuffer.EndSample("Temporal");*/
        }
    }
}
