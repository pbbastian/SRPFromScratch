using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace SRPWorkshop.Part1
{
    public class ScratchPipeline : RenderPipeline
    {
        // We store cull results in an instance variable to avoid GC allocations per-frame, as they are a killer for
        // performance.
        CullResults m_CullResults;
        
        // This is where the rendering happens. Notice that we are given a ScriptableRenderContext to perform rendering
        // with, as well as an array of cameras to render. Note that the ScriptableRenderContext works using a deferred
        // execution model, and rendering calls won't happen until we submit them.
        public override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            // The base class will do some checks for us, so remember to call into that one first.
            base.Render(context, cameras);

            // Loop over each camera to perform rendering for it.
            foreach (var camera in cameras)
            {
                // Perform culling using the current camera. The result of this is a handle to visible renderers (i.e.
                // Game Objects with a Renderer component), as well as lists of visible lights, reflection probes etc. 
                if (!CullResults.Cull(camera, context, out m_CullResults))
                {
                    // If the culling fails, we ignore this camera and continue on to the next one.
                    continue;
                }
                
                // This call makes the context extract various properties from the camera, and use it to set-up things
                // like render targets and shader variables. 
                context.SetupCameraProperties(camera);

                {
                    // A lot of things in SRP happens through CommandBuffers. Since they are classes we cannot create
                    // new instances of them without allocating GC memory. To avoid that we can use the
                    // CommandBufferPool to get an empty, existing CommandBuffer. We can tag each command buffer with a
                    // name, which will show up both in the Frame Debugger, and RenderDoc.
                    var cmd = CommandBufferPool.Get("Clear");
                    // Clear the current color buffer to black, as well as the current depth buffer.
                    cmd.ClearRenderTarget(true, true, Color.black);
                    // Execute the command buffer via the ScriptableRenderContext.
                    context.ExecuteCommandBuffer(cmd);
                    // While the execution doesn't happen immediately, the command buffer _is_ copied into internal
                    // storage, and so we can safely release it back to the pool.
                    CommandBufferPool.Release(cmd);
                }
                
                // Finally, we submit the calls we made for immediate execution. Note that you can do this any time you
                // want to. Typically it makes sense to do it per camera, but some times you might want to break it up.
                context.Submit();
            }
        }
    }
}
