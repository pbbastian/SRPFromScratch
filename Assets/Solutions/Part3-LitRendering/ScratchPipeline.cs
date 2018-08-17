// This is heavily based on ScratchPipeline.cs in Part 2. Make sure to view the comments in that file as well, as the
// comments in this one will only cover new/changed lines of code.

using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace SRPWorkshop.Part3
{
    public class ScratchPipeline : RenderPipeline
    {
        CullResults m_CullResults;
        // This buffer will hold the list of visible lights. We will make this buffer available for use in our shader,
        // so that we can light our objects.
        ComputeBuffer m_LightBuffer;

        // We need to initialize the light buffer
        public ScratchPipeline()
        {
            // Our data is 2 x float4. Each float is 4 bytes, thus the stride must be 4*4*2. 
            // We fix the maximum amount of lights to 64 for simplicity.
            // We're allocating this up front as it would be expensive to do per frame.
            m_LightBuffer = new ComputeBuffer(64, 4*4*2);
        }

        // We now have resources to clean up, so we need to override the Dispose method. This will be called when the
        // render pipeline instance gets destroyed by Unity. This could happen e.g. if the pipeline asset is changed, or
        // if the game just ends.
        public override void Dispose()
        {
            base.Dispose();
            // This will properly clean up the light buffer we allocated before.
            m_LightBuffer.Dispose();
        }

        public override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            base.Render(context, cameras);

            foreach (var camera in cameras)
            {
                if (!CullResults.Cull(camera, context, out m_CullResults))
                {
                    continue;
                }
                
                context.SetupCameraProperties(camera);

                {
                    var cmd = CommandBufferPool.Get("Clear");
                    cmd.ClearRenderTarget(true, true, Color.black);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }

                {
                    var cmd = CommandBufferPool.Get("Set-up Light Buffer");

                    var lightCount = m_CullResults.visibleLights.Count;
                    // We need to build up the data we want to put into the ComputeBuffer, as we cannot write directly
                    // to it. We use a native array for this, as the allocation will be extremely cheap when using the
                    // temp allocator.
                    // Each light uses 2 x float4 values in the shader, so we allocate 2 x Vector4 per light. You could
                    // also create a C# struct that mirrors the HLSL struct, but be careful wrt. packing rules.
                    var lightArray = new NativeArray<Vector4>(lightCount * 2, Allocator.Temp);

                    // Loop over all the lights and fill up the light buffer.
                    for (var i = 0; i < lightCount; i++)
                    {
                        var light = m_CullResults.visibleLights[i];
                        
                        // Let's decide what to put in the first float4. Depending on whether it's a point light or a
                        // directional light, we store position or direction in here, respectively.
                        // Note that you can choose your own format if you want to. Just make sure it maches on both the
                        // shader side and the C# side.
                        Vector4 lightData;
                        if (light.lightType == LightType.Directional)
                        {
                            // If it's a directional light we store direction in the xyz components, and a negative
                            // value in the w component. This allows us to identify whether it is a directional light.
                            lightData = light.localToWorld.MultiplyVector(Vector3.back);
                            lightData.w = -1; 
                        }
                        else if (light.lightType == LightType.Point)
                        {
                            // If it's a point light we store position in the xyz components, and range in the w
                            // component.
                            lightData = light.localToWorld.GetColumn(3);
                            lightData.w = light.range;
                        }
                        else
                        {
                            // If it's not a point light or a directional light, we ignore the light.
                            continue;
                        }

                        // Finally we put the values into the light buffer.
                        lightArray[i * 2] = lightData;
                        lightArray[i * 2 + 1] = light.finalColor;
                    }
                    
                    // Now that our native array with light data is all filled up, we put it into the light buffer.
                    m_LightBuffer.SetData(lightArray);
                    // We can now now safely dispose of the light array. This is important as we would otherwise leak
                    // memory.
                    lightArray.Dispose();
                    
                    // Finally, make it available for use in shaders.
                    cmd.SetGlobalBuffer("_LightBuffer", m_LightBuffer);
                    cmd.SetGlobalInt("_LightCount", lightCount);
                    
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }

                var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("Forward"));
                var filterSettings = new FilterRenderersSettings(true);
                
                drawSettings.sorting.flags = SortFlags.CommonOpaque;
                filterSettings.renderQueueRange = RenderQueueRange.opaque;
                context.DrawRenderers(m_CullResults.visibleRenderers, ref drawSettings, filterSettings);
                
                drawSettings.sorting.flags = SortFlags.CommonTransparent;
                filterSettings.renderQueueRange = RenderQueueRange.transparent;
                context.DrawRenderers(m_CullResults.visibleRenderers, ref drawSettings, filterSettings);
                
                context.Submit();
            }
        }
    }
}
