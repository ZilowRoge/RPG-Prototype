using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace OutlineURP
{
    public sealed class OutlineRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private OutlineProfile profile;

        [SerializeField]
        private RenderPassEvent maskPassEvent = RenderPassEvent.AfterRenderingTransparents;

        [SerializeField]
        private RenderPassEvent compositePassEvent = RenderPassEvent.AfterRenderingPostProcessing;

        [SerializeField]
        private Shader maskShader;

        [SerializeField]
        private Shader compositeShader;

        [SerializeField]
        private bool debugShowMask;

        [SerializeField]
        private bool debugForceFullscreenTint;

        [SerializeField]
        private Color debugFullscreenTintColor = new(1f, 0f, 1f, 1f);

        private Material maskMaterial;
        private Material compositeMaterial;
        private MaskPass maskPass;
        private CompositePass compositePass;

        public override void Create()
        {
            if (maskShader == null)
            {
                maskShader = Shader.Find("Hidden/OutlineURP/Mask");
            }

            if (compositeShader == null)
            {
                compositeShader = Shader.Find("Hidden/OutlineURP/Composite");
            }

            CoreUtils.Destroy(maskMaterial);
            CoreUtils.Destroy(compositeMaterial);

            if (maskShader != null)
            {
                maskMaterial = CoreUtils.CreateEngineMaterial(maskShader);
            }

            if (compositeShader != null)
            {
                compositeMaterial = CoreUtils.CreateEngineMaterial(compositeShader);
            }

            maskPass = new MaskPass
            {
                renderPassEvent = maskPassEvent
            };

            compositePass = new CompositePass
            {
                renderPassEvent = compositePassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            OutlineDebugStats.LastAddRenderPassesFrame = Time.frameCount;

            if (!CanRender(in renderingData) || maskPass == null || compositePass == null || maskMaterial == null || compositeMaterial == null)
            {
                return;
            }

            ConfigurePasses();

            maskPass.renderPassEvent = maskPassEvent;
            compositePass.renderPassEvent = debugForceFullscreenTint ? RenderPassEvent.AfterRendering : compositePassEvent;

            if (debugForceFullscreenTint)
            {
                renderer.EnqueuePass(compositePass);
                return;
            }

            if (profile == null)
            {
                return;
            }

            if (!OutlineRegistry.HasAnyActive)
            {
                return;
            }

            renderer.EnqueuePass(maskPass);
            renderer.EnqueuePass(compositePass);
        }

        protected override void Dispose(bool disposing)
        {
            maskPass?.Dispose();
            maskPass = null;
            compositePass = null;
            CoreUtils.Destroy(maskMaterial);
            CoreUtils.Destroy(compositeMaterial);
        }

        private void ConfigurePasses()
        {
            maskPass?.Setup(maskMaterial, profile);
            compositePass?.Setup(compositeMaterial, profile, maskPass);
            compositePass?.SetDebugShowMask(debugShowMask);
            compositePass?.SetDebugForceFullscreenTint(debugForceFullscreenTint, debugFullscreenTintColor);
        }

        private static bool CanRender(in RenderingData renderingData)
        {
            var type = renderingData.cameraData.cameraType;
            return type != CameraType.Preview && type != CameraType.Reflection;
        }

        private sealed class MaskPass : ScriptableRenderPass
        {
            private static readonly int OutlineMaskColor = Shader.PropertyToID("_OutlineMaskColor");
            private static readonly int ZTest = Shader.PropertyToID("_ZTest");

            private readonly ProfilingSampler sampler = new("OutlineMaskPass");
            private RTHandle maskTexture;
            private TextureHandle maskTextureHandle;
            private Material material;
            private OutlineProfile profile;

            public RTHandle MaskTexture => maskTexture;
            public TextureHandle MaskTextureHandle => maskTextureHandle;

            public void Setup(Material maskMaterial, OutlineProfile outlineProfile)
            {
                material = maskMaterial;
                profile = outlineProfile;
            }

            public void Dispose()
            {
                maskTexture?.Release();
                maskTexture = null;
            }

            [System.Obsolete("Compatibility mode only (Render Graph disabled).", false)]
            public void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                if (material == null || profile == null)
                {
                    return;
                }

                var descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.msaaSamples = 1;
                descriptor.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
                descriptor.depthStencilFormat = GraphicsFormat.None;
                descriptor.useMipMap = false;
                descriptor.autoGenerateMips = false;
                descriptor.enableRandomWrite = false;

                RenderingUtils.ReAllocateHandleIfNeeded(ref maskTexture, descriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_OutlineMaskTexture");
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                OutlineDebugStats.LastMaskRecordGraphFrame = Time.frameCount;
                maskTextureHandle = TextureHandle.nullHandle;
                if (material == null || profile == null || !OutlineRegistry.HasAnyActive)
                {
                    return;
                }

                var resourceData = frameData.Get<UniversalResourceData>();
                if (!resourceData.activeColorTexture.IsValid())
                {
                    return;
                }

                var textureDesc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
                textureDesc.name = "_OutlineMaskTextureRG";
                textureDesc.clearBuffer = true;
                textureDesc.clearColor = Color.clear;
                textureDesc.colorFormat = GraphicsFormat.R8G8B8A8_UNorm;
                textureDesc.depthBufferBits = DepthBits.None;
                textureDesc.msaaSamples = MSAASamples.None;
                textureDesc.useMipMap = false;
                textureDesc.autoGenerateMips = false;
                textureDesc.enableRandomWrite = false;

                maskTextureHandle = renderGraph.CreateTexture(textureDesc);

                using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>(passName, out var passData, profilingSampler))
                {
                    passData.material = material;
                    passData.profile = profile;
                    passData.maskTexture = maskTextureHandle;

                    builder.SetRenderAttachment(passData.maskTexture, 0, AccessFlags.WriteAll);
                    if (resourceData.activeDepthTexture.IsValid())
                    {
                        builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                    }

                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc((MaskPassData data, RasterGraphContext rgContext) =>
                    {
                        ExecuteMaskPass(rgContext, data);
                    });
                }
            }

            [System.Obsolete("Compatibility mode only (Render Graph disabled).", false)]
            public void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                OutlineDebugStats.LastMaskExecuteCompatFrame = Time.frameCount;
                if (material == null || profile == null || maskTexture == null)
                {
                    return;
                }

                var entries = OutlineRegistry.Entries;
                if (entries.Count == 0)
                {
                    return;
                }

                var commandBuffer = CommandBufferPool.Get("OutlineMaskPass");
                using (new ProfilingScope(commandBuffer, sampler))
                {
                    var occlusionMode = OutlineController.ResolveOcclusionMode(profile.DefaultOcclusionMode);
                    var zTestValue = occlusionMode == OutlineOcclusionMode.RespectDepth
                        ? (int)CompareFunction.LessEqual
                        : (int)CompareFunction.Always;

                    material.SetInt(ZTest, zTestValue);
                    // URP 17+ no longer exposes camera depth/color target handles in this path.
                    // Keep compatibility mode functional by rendering mask color only.
                    CoreUtils.SetRenderTarget(commandBuffer, maskTexture, ClearFlag.Color, Color.clear);

                    for (var i = 0; i < entries.Count; i++)
                    {
                        var entry = entries[i];
                        if (entry.renderer == null || !entry.renderer.enabled || !entry.renderer.gameObject.activeInHierarchy)
                        {
                            continue;
                        }

                        var color = profile.GetColor(entry.group, entry.state);
                        color.a = 1f;
                        commandBuffer.SetGlobalColor(OutlineMaskColor, color);

                        var materials = entry.renderer.sharedMaterials;
                        var subMeshCount = materials != null && materials.Length > 0 ? materials.Length : 1;
                        for (var subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
                        {
                            commandBuffer.DrawRenderer(entry.renderer, material, subMeshIndex, 0);
                        }
                    }
                }

                context.ExecuteCommandBuffer(commandBuffer);
                CommandBufferPool.Release(commandBuffer);
            }

            private static void ExecuteMaskPass(RasterGraphContext context, MaskPassData data)
            {
                OutlineDebugStats.LastMaskExecuteGraphFrame = Time.frameCount;
                var entries = OutlineRegistry.Entries;
                if (entries.Count == 0)
                {
                    return;
                }

                var occlusionMode = OutlineController.ResolveOcclusionMode(data.profile.DefaultOcclusionMode);
                var zTestValue = occlusionMode == OutlineOcclusionMode.RespectDepth
                    ? (int)CompareFunction.LessEqual
                    : (int)CompareFunction.Always;

                data.material.SetInt(ZTest, zTestValue);
                context.cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear, 1, 0);

                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if (entry.renderer == null || !entry.renderer.enabled || !entry.renderer.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    var color = data.profile.GetColor(entry.group, entry.state);
                    color.a = 1f;
                    context.cmd.SetGlobalColor(OutlineMaskColor, color);

                    var materials = entry.renderer.sharedMaterials;
                    var subMeshCount = materials != null && materials.Length > 0 ? materials.Length : 1;
                    for (var subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
                    {
                        context.cmd.DrawRenderer(entry.renderer, data.material, subMeshIndex, 0);
                    }
                }
            }

            private sealed class MaskPassData
            {
                internal Material material;
                internal OutlineProfile profile;
                internal TextureHandle maskTexture;
            }
        }

        private sealed class CompositePass : ScriptableRenderPass
        {
            private static readonly int OutlineThickness = Shader.PropertyToID("_OutlineThickness");
            private static readonly int OutlineDebugShowMask = Shader.PropertyToID("_OutlineDebugShowMask");
            private static readonly int OutlineDebugForceFullscreen = Shader.PropertyToID("_OutlineDebugForceFullscreen");
            private static readonly int OutlineDebugForceColor = Shader.PropertyToID("_OutlineDebugForceColor");
            private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
            private static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");

            private readonly ProfilingSampler sampler = new("OutlineCompositePass");
            private Material material;
            private OutlineProfile profile;
            private MaskPass maskPass;
            private bool debugShowMask;
            private bool debugForceFullscreenTint;
            private Color debugForceFullscreenTintColor;

            public void Setup(Material compositeMaterial, OutlineProfile outlineProfile, MaskPass sourceMaskPass)
            {
                material = compositeMaterial;
                profile = outlineProfile;
                maskPass = sourceMaskPass;
            }

            public void SetDebugShowMask(bool enabled)
            {
                debugShowMask = enabled;
            }

            public void SetDebugForceFullscreenTint(bool enabled, Color color)
            {
                debugForceFullscreenTint = enabled;
                debugForceFullscreenTintColor = color;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                OutlineDebugStats.LastCompositeRecordGraphFrame = Time.frameCount;
                var maskTexture = maskPass != null ? maskPass.MaskTextureHandle : TextureHandle.nullHandle;
                var forceFullscreen = debugForceFullscreenTint;
                if (material == null)
                {
                    return;
                }

                if (!forceFullscreen && (!maskTexture.IsValid() || !OutlineRegistry.HasAnyActive))
                {
                    return;
                }

                var resourceData = frameData.Get<UniversalResourceData>();
                using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>(passName, out var passData, profilingSampler))
                {
                    passData.material = material;
                    passData.maskTexture = maskTexture;
                    passData.destination = resourceData.activeColorTexture;
                    passData.thickness = profile != null ? profile.Thickness : 2f;
                    passData.debugShowMask = debugShowMask ? 1f : 0f;
                    passData.debugForceFullscreen = debugForceFullscreenTint ? 1f : 0f;
                    passData.debugForceColor = debugForceFullscreenTintColor;

                    if (!forceFullscreen)
                    {
                        builder.UseTexture(passData.maskTexture, AccessFlags.Read);
                    }
                    builder.SetRenderAttachment(passData.destination, 0, AccessFlags.ReadWrite);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);

                    builder.SetRenderFunc((CompositePassData data, RasterGraphContext context) =>
                    {
                        OutlineDebugStats.LastCompositeExecuteGraphFrame = Time.frameCount;
                        data.material.SetFloat(OutlineThickness, data.thickness);
                        data.material.SetFloat(OutlineDebugShowMask, data.debugShowMask);
                        data.material.SetFloat(OutlineDebugForceFullscreen, data.debugForceFullscreen);
                        data.material.SetColor(OutlineDebugForceColor, data.debugForceColor);

                        if (data.debugForceFullscreen > 0.5f)
                        {
                            context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1);
                            return;
                        }

                        context.cmd.SetGlobalTexture(BlitTextureId, data.maskTexture);
                        context.cmd.SetGlobalVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));
                        context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1);
                    });
                }
            }

            [System.Obsolete("Compatibility mode only (Render Graph disabled).", false)]
            public void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                OutlineDebugStats.LastCompositeExecuteCompatFrame = Time.frameCount;
                var maskTexture = maskPass?.MaskTexture;
                if (material == null)
                {
                    return;
                }

                if (!debugForceFullscreenTint && (maskTexture == null || !OutlineRegistry.HasAnyActive))
                {
                    return;
                }

                var commandBuffer = CommandBufferPool.Get("OutlineCompositePass");
                using (new ProfilingScope(commandBuffer, sampler))
                {
                    material.SetFloat(OutlineThickness, profile != null ? profile.Thickness : 2f);
                    material.SetFloat(OutlineDebugShowMask, debugShowMask ? 1f : 0f);
                    material.SetFloat(OutlineDebugForceFullscreen, debugForceFullscreenTint ? 1f : 0f);
                    material.SetColor(OutlineDebugForceColor, debugForceFullscreenTintColor);

                    if (debugForceFullscreenTint)
                    {
                        commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                        commandBuffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3, 1);
                    }
                    else
                    {
                        commandBuffer.SetGlobalTexture(BlitTextureId, maskTexture);
                        commandBuffer.SetGlobalVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));
                        commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                        commandBuffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3, 1);
                    }
                }

                context.ExecuteCommandBuffer(commandBuffer);
                CommandBufferPool.Release(commandBuffer);
            }

            private sealed class CompositePassData
            {
                internal Material material;
                internal TextureHandle maskTexture;
                internal TextureHandle destination;
                internal float thickness;
                internal float debugShowMask;
                internal float debugForceFullscreen;
                internal Color debugForceColor;
            }
        }
    }
}
