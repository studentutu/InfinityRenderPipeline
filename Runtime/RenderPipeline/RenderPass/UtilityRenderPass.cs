﻿using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering;
using InfinityTech.Rendering.RDG;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InfinityTech.Rendering.Pipeline
{
    internal struct FUtilityPassString
    {
        internal static string SkyBoxPassName = "Gizmos";
        internal static string GizmosPassName = "SkyBox";
        internal static string PresentPassName = "Present";
    }

    public partial class InfinityRenderPipeline
    {

        ///////////Gizmos Graph
        struct GizmosPassData
        {
        #if UNITY_EDITOR
            public Camera camera;
            public GizmoSubset gizmoSubset;
        #endif
        }

#if UNITY_EDITOR
        void RenderGizmos(Camera camera, GizmoSubset gizmoSubset)
        {
            if (Handles.ShouldRenderGizmos())
            {
                // Add GizmosPass
                using (RDGPassRef passRef = m_GraphBuilder.AddPass<GizmosPassData>(FUtilityPassString.GizmosPassName, ProfilingSampler.Get(CustomSamplerId.RenderGizmos)))
                {
                    //Setup Phase
                    ref GizmosPassData passData = ref passRef.GetPassData<GizmosPassData>();
                    passData.camera = camera;
                    passData.gizmoSubset = gizmoSubset;

                    //Execute Phase
                    passRef.SetExecuteFunc((ref GizmosPassData passData, ref RDGContext graphContext) =>
                    {
                        graphContext.renderContext.DrawGizmos(passData.camera, passData.gizmoSubset);
                    });
                }
            }
        }
#endif

        ///////////SkyBox Graph
        struct SkyBoxPassData
        {
            public Camera camera;
        }

        void RenderSkyBox(Camera camera)
        {
            // Add SkyBoxPass
            using (RDGPassRef passRef = m_GraphBuilder.AddPass<SkyBoxPassData>(FUtilityPassString.SkyBoxPassName, ProfilingSampler.Get(CustomSamplerId.RenderSkyBox)))
            {
                //Setup Phase
                ref SkyBoxPassData passData = ref passRef.GetPassData<SkyBoxPassData>();
                passData.camera = camera;

                //Execute Phase
                passRef.SetExecuteFunc((ref SkyBoxPassData passData, ref RDGContext graphContext) =>
                {
                    graphContext.renderContext.DrawSkybox(passData.camera);
                });
            }
        }

        ///////////Present Graph
        struct PresentPassData
        {
            public Camera camera;
            public RDGTextureRef srcTexture;
            public RenderTexture dscTexture;
        }

        void RenderPresent(Camera camera, RenderTexture dscTexture)
        {
            RDGTextureRef srcTexture = m_GraphBuilder.ScopeTexture(InfinityShaderIDs.AntiAliasingBuffer);
            
            // Add PresentPass
            using (RDGPassRef passRef = m_GraphBuilder.AddPass<PresentPassData>(FUtilityPassString.PresentPassName, ProfilingSampler.Get(CustomSamplerId.RenderPresent)))
            {
                //Setup Phase
                ref PresentPassData passData = ref passRef.GetPassData<PresentPassData>();
                passData.camera = camera;
                passData.dscTexture = dscTexture;
                passData.srcTexture = passRef.ReadTexture(srcTexture);

                //Execute Phase
                passRef.SetExecuteFunc((ref PresentPassData passData, ref RDGContext graphContext) =>
                {
                    RenderTexture srcBuffer = passData.srcTexture;
                    RenderTexture dscBuffer = passData.dscTexture;

                    float4 ScaleBias = new float4((float)passData.camera.pixelWidth / (float)srcBuffer.width, (float)passData.camera.pixelHeight / (float)srcBuffer.height, 0.0f, 0.0f);
                    if (!passData.dscTexture) { ScaleBias.w = ScaleBias.y; ScaleBias.y *= -1; }

                    graphContext.cmdBuffer.SetGlobalVector(InfinityShaderIDs.ScaleBias, ScaleBias);
                    graphContext.cmdBuffer.DrawFullScreen(GraphicsUtility.GetViewport(passData.camera), srcBuffer, new RenderTargetIdentifier(dscBuffer), 1);
                });
            }
        }
    }
}
