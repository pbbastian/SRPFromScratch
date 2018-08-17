// See ScratchPipelineAsset.cs in Part 1 for detailed comments. No changes have been made in this one other than
// changing the number in MenuItem.
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine.Experimental.Rendering;

namespace SRPWorkshop.Part2
{
    public class ScratchPipelineAsset : RenderPipelineAsset
    {
        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new ScratchPipeline();
        }
        
#if UNITY_EDITOR
        [MenuItem("Assets/Create/Rendering/ScratchRP Part 2", priority = CoreUtils.assetCreateMenuPriority1)]
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
