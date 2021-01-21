using Unity.Jobs;
using UnityEngine;
using Unity.Collections;
using InfinityTech.Core.Geometry;

namespace InfinityTech.Rendering.MeshPipeline
{
    public struct FCullingData
    {
        private JobHandle CullingJobRef;
        private NativeArray<FPlane> ViewFrustum;
        public NativeList<int> ViewMeshBatchs;

        public void DoCull(Camera RenderCamera, NativeArray<FMeshBatch> MeshBatchs, in bool bParallel = false)
        {
            ViewFrustum = new NativeArray<FPlane>(6, Allocator.TempJob);
            Plane[] FrustumPlane = GeometryUtility.CalculateFrustumPlanes(RenderCamera);
            for (int PlaneIndex = 0; PlaneIndex < 6; PlaneIndex++)
            {
                ViewFrustum[PlaneIndex] = FrustumPlane[PlaneIndex];
            }

            ViewMeshBatchs = new NativeList<int>(MeshBatchs.Length, Allocator.TempJob);
            ViewMeshBatchs.Resize(MeshBatchs.Length, NativeArrayOptions.ClearMemory);

            FMarkMeshBatchCullJob MarkCullingJob = new FMarkMeshBatchCullJob();
            {
                MarkCullingJob.ViewFrustum = ViewFrustum;
                MarkCullingJob.MeshBatchs = MeshBatchs;
                MarkCullingJob.ViewMeshBatchs = ViewMeshBatchs;
            }
            CullingJobRef = MarkCullingJob.Schedule(MeshBatchs.Length, 256);

            if (bParallel) { JobHandle.ScheduleBatchedJobs(); }
        }

        public void Sync()
        {
            CullingJobRef.Complete();
        }

        public void Release()
        {
            ViewFrustum.Dispose();
            ViewMeshBatchs.Dispose();
        }
    }
}