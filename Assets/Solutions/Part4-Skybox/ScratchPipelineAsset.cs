// See ScratchPipelineAsset.cs in Part 1 for detailed comments.
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace SRPWorkshop.Part4
{
    public class ScratchPipelineAsset : RenderPipelineAsset
    {
        // Add a public field for storing the copy depth material. This lets the user pick one in the inspector.
        // Explicitly initialize to avoid C# compiler warning. 
        // You could also store a Shader here, and then create the Material in the render pipeline. Then you would need
        // to clean it up afterwards though.
        public Material copyDepthMaterial = null;
        
        protected override IRenderPipeline InternalCreatePipeline()
        {
            // We now pass a reference to the asset, so that the render pipeline can grab the material.
            return new ScratchPipeline(this);
        }
        
#if UNITY_EDITOR
        [MenuItem("Assets/Create/Rendering/ScratchRP Part 4", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateBasicRenderPipeline()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateScratchPipelineAsset>(),
                "ScratchAsset.asset", null, null);
        }
        
        class CreateScratchPipelineAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<ScratchPipelineAsset>();
                AssetDatabase.CreateAsset(instance, pathName);
            }
        }
#endif
    }
}
