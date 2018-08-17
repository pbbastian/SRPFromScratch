// See ScratchPipelineAsset.cs in Part 1 for detailed comments.
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace SRPWorkshop.Part5
{
    public class ScratchPipelineAsset : RenderPipelineAsset
    {
        public Material copyDepthMaterial = null;
        // We also need a deferred material now.
        public Material deferredMaterial = null;
        
        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new ScratchPipeline(this);
        }
        
#if UNITY_EDITOR
        [MenuItem("Assets/Create/Rendering/ScratchRP Part 5", priority = CoreUtils.assetCreateMenuPriority1)]
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
