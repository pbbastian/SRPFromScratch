// This is heavily based on ScratchPipeline.cs in Part 1. Make sure to view the comments in that file as well, as the
// comments in this one will only cover new/changed lines of code.
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace SRPWorkshop.Part2
{
    public class ScratchPipeline : RenderPipeline
    {
        CullResults m_CullResults;
        
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

                // SRP doesn't allow us to 100% control drawing of renderers, but we can configure a lot of things
                // via the DrawRendererSettings and FilterRenderersSettings.
                // Here we specify the camera we want to use, and that we want to use the "Forward" shader pass. In the
                // shader source this filters passes based on the "LightMode" tag.
                var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("Forward"));
                // The `true` passed to the constructor here will initalize values in the struct to sensible defaults.
                var filterSettings = new FilterRenderersSettings(true);
                
                // We want to use typical opaque sorting.
                drawSettings.sorting.flags = SortFlags.CommonOpaque;
                // We specify the render queue range to only include opaque objects, thus filtering out e.g. transparent
                // objects.
                filterSettings.renderQueueRange = RenderQueueRange.opaque;
                // We can now draw the renderers using the settings we just set up.
                context.DrawRenderers(m_CullResults.visibleRenderers, ref drawSettings, filterSettings);
                
                // Draw transparent renderers by setting the sorting flags and render queue range similarly to how we
                // did for the opaque renderers. 
                drawSettings.sorting.flags = SortFlags.CommonTransparent;
                filterSettings.renderQueueRange = RenderQueueRange.transparent;
                context.DrawRenderers(m_CullResults.visibleRenderers, ref drawSettings, filterSettings);
                
                context.Submit();
            }
        }
    }
}
