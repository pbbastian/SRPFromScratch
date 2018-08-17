// This is heavily based on ScratchPipeline.cs in Part 3. Make sure to view the comments in that file as well, as the
// comments in this one will only cover new/changed lines of code.
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace SRPWorkshop.Part4
{
    public class ScratchPipeline : RenderPipeline
    {
        CullResults cullResults;
        ComputeBuffer lightBuffer;
        Material copyDepthMaterial;

        // We now need to grab a value from the asset. We do this on construction so that we don't have to worry about
        // changes in our render method.
        public ScratchPipeline(ScratchPipelineAsset pipelineAsset)
        {
            lightBuffer = new ComputeBuffer(64, 4*4*2);
            copyDepthMaterial = pipelineAsset.copyDepthMaterial;
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

                // PropertyToID will return an ID that uniquely identifies the property name. The function we're going
                // to use later on requires us to use these IDs, so we compute them here.
                // You could also pre-calculate these, but for simplicity we're keeping them here.
                // We need a color and a depth render target.
                var colorRT = Shader.PropertyToID("_ColorRT");
                var colorRTID = new RenderTargetIdentifier(colorRT);
                var depthRT = Shader.PropertyToID("_CameraDepthTexture");
                var depthRTID = new RenderTargetIdentifier(depthRT);
                {
                    var cmd = CommandBufferPool.Get("Set-up Render Targets");
                    // GetTemporaryRT gets us a temporary render target we can render into. Internally Unity re-uses
                    // these as they get returned to the pool.
                    // Get our color render target. No need for any depth bits as we're getting a separate depth buffer.
                    cmd.GetTemporaryRT(colorRT, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, RenderTextureFormat.ARGB32);
                    // Get our depth render target.
                    cmd.GetTemporaryRT(depthRT, camera.pixelWidth, camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
                    // We can now set the active render target to our newly acquired render targets.
                    // Important: Do _not_ use the implicit conversion from `int` to `RenderTargetIdentifier`. This will
                    // sometimes silently select the wrong overload of `SetRenderTarget` for you, leading to pain and
                    // suffering.
                    cmd.SetRenderTarget(colorRTID, depthRTID);
                    // Clear like before, but now with our new render targets.
                    cmd.ClearRenderTarget(true, true, Color.black);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }

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

                var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("Forward"));
                var filterSettings = new FilterRenderersSettings(true);
                
                drawSettings.sorting.flags = SortFlags.CommonOpaque;
                filterSettings.renderQueueRange = RenderQueueRange.opaque;
                context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, filterSettings);
                
                // Draw the skybox. Note that you could also do your completely own thing here, and not use the built-in
                // skybox. It's important that this happens after opaque rendering, but before transparent. The
                // built-in skybox uses the depth buffer to only draw where no other objects draw.
                context.DrawSkybox(camera);
                
                drawSettings.sorting.flags = SortFlags.CommonTransparent;
                filterSettings.renderQueueRange = RenderQueueRange.transparent;
                context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, filterSettings);

                // Copy the depth from our own depth render target into the camera target.
                {
                    var cmd = CommandBufferPool.Get("Copy Depth");
                    // Blit does a fullscreen pass. We're using the same input as output, because we're binding the
                    // input ourselves, but Unity doesn't like a null value here. We use the copy depth material we set
                    // in the asset.
                    cmd.Blit(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget, copyDepthMaterial);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }

                {
                    var cmd = CommandBufferPool.Get("Final Blit");
                    cmd.Blit(colorRT, BuiltinRenderTextureType.CameraTarget);
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
