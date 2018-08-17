// This is heavily based on ScratchPipeline.cs in Part 4. Make sure to view the comments in that file as well, as the
// comments in this one will only cover new/changed lines of code.

using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace SRPWorkshop.Part5
{
    public class ScratchPipeline : RenderPipeline
    {
        CullResults cullResults;
        ComputeBuffer lightBuffer;
        Material copyDepthMaterial;
        Material deferredMaterial;
        // Pre-allocate array for use with SetRenderTarget.
        RenderTargetIdentifier[] gbufferRTIDs = new RenderTargetIdentifier[2];

        public ScratchPipeline(ScratchPipelineAsset pipelineAsset)
        {
            lightBuffer = new ComputeBuffer(64, 4*4*2);
            copyDepthMaterial = pipelineAsset.copyDepthMaterial;
            deferredMaterial = pipelineAsset.deferredMaterial;
        }

        public override void Dispose()
        {
            base.Dispose();
            lightBuffer.Dispose();
        }

        public override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            base.Render(context, cameras);

            foreach (var camera in cameras)
            {
                if (!CullResults.Cull(camera, context, out cullResults))
                {
                    continue;
                }
                
                context.SetupCameraProperties(camera);

                // Declare the shader properties for the GBuffer render targets.
                // GBuffer0 contains albedo in the rgb channels, and nothing in the a channel.
                var gbuffer0RT = Shader.PropertyToID("_GBuffer0");
                var gbuffer0RTID = new RenderTargetIdentifier(gbuffer0RT);
                // GBuffer1 contains normal in the rgb channels, and nothing in the a channel.
                var gbuffer1RT = Shader.PropertyToID("_GBuffer1");
                var gbuffer1RTID = new RenderTargetIdentifier(gbuffer1RT);
                
                var colorRT = Shader.PropertyToID("_ColorRT");
                var colorRTID = new RenderTargetIdentifier(colorRT);
                var depthRT = Shader.PropertyToID("_CameraDepthTexture");
                var depthRTID = new RenderTargetIdentifier(depthRT);
                {
                    var cmd = CommandBufferPool.Get("Set-up Render Targets");
                    // Get temporary render targets for our GBuffer like before.
                    cmd.GetTemporaryRT(gbuffer0RT, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGB32);
                    // It might make sense to use a different texture format for e.g. normals, like we do here.
                    cmd.GetTemporaryRT(gbuffer1RT, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGB2101010);
                    cmd.GetTemporaryRT(colorRT, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGB32);
                    cmd.GetTemporaryRT(depthRT, camera.pixelWidth, camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
                    // Note that we want to render into the GBuffer render targets first. We can pass an array of color
                    // render targets instead of a single one. We still use the same depth buffer.
                    gbufferRTIDs[0] = gbuffer0RTID;
                    gbufferRTIDs[1] = gbuffer1RTID;
                    cmd.SetRenderTarget(gbufferRTIDs, depthRTID);
                    cmd.ClearRenderTarget(true, true, Color.black);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }

                // Note that we want to draw using the GBuffer pass first. We don't need to set up lights at this point.
                var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("GBuffer"));
                var filterSettings = new FilterRenderersSettings(true);
                
                // The rest of our GBuffer rendering setup is the same as before.
                drawSettings.sorting.flags = SortFlags.CommonOpaque;
                filterSettings.renderQueueRange = RenderQueueRange.opaque;
                context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, filterSettings);

                // Now that we have filled the GBuffer it's time to set up the light buffer like before.
                {
                    var cmd = CommandBufferPool.Get("Set-up Light Buffer");

                    var lightCount = cullResults.visibleLights.Count;
                    var lightArray = new NativeArray<Vector4>(lightCount * 2, Allocator.Temp);

                    for (var i = 0; i < lightCount; i++)
                    {
                        var light = cullResults.visibleLights[i];
                        
                        Vector4 lightData;
                        if (light.lightType == LightType.Directional)
                        {
                            lightData = light.localToWorld.MultiplyVector(Vector3.back);
                            lightData.w = -1; 
                        }
                        else if (light.lightType == LightType.Point)
                        {
                            lightData = light.localToWorld.GetColumn(3);
                            lightData.w = light.range;
                        }
                        else
                        {
                            continue;
                        }

                        lightArray[i * 2] = lightData;
                        lightArray[i * 2 + 1] = light.finalColor;
                    }
                    
                    lightBuffer.SetData(lightArray);
                    lightArray.Dispose();
                    
                    cmd.SetGlobalBuffer("_LightBuffer", lightBuffer);
                    cmd.SetGlobalInt("_LightCount", lightCount);
                    
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }

                {
                    var cmd = CommandBufferPool.Get("Deferred");
                    // Blit using our deferred material. This works like the depth copy in part 4.
                    cmd.Blit(colorRTID, colorRTID, deferredMaterial);
                    // Switch back to rendering into the color render target. This is important to do _after_ the blit,
                    // as the blit will change the render targets.
                    cmd.SetRenderTarget(colorRTID, depthRTID);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }

                // Draw the skybox again. At this point the color buffer contains the same as after opaque rendering
                // in part 4.
                {
                    var cmd = CommandBufferPool.Get("Skybox");
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                    context.DrawSkybox(camera);
                }
                
                // We now draw the opaque objects like before. This is still using the Forward pass.
                drawSettings = new DrawRendererSettings(camera, new ShaderPassName("Forward"));
                drawSettings.sorting.flags = SortFlags.CommonTransparent;
                filterSettings.renderQueueRange = RenderQueueRange.transparent;
                context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, filterSettings);

                {
                    var cmd = CommandBufferPool.Get("Copy Depth");
                    cmd.Blit(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget, copyDepthMaterial);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }

                {
                    var cmd = CommandBufferPool.Get("Final Blit");
                    cmd.Blit(colorRT, BuiltinRenderTextureType.CameraTarget);
                    // The GBuffer render targets must also be released.
                    cmd.ReleaseTemporaryRT(gbuffer0RT);
                    cmd.ReleaseTemporaryRT(gbuffer1RT);
                    cmd.ReleaseTemporaryRT(colorRT);
                    cmd.ReleaseTemporaryRT(depthRT);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
                
                context.Submit();
            }
        }
    }
}
