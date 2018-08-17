using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine.Experimental.Rendering;

namespace SRPWorkshop.Part1
{
    // The class that represents the asset for our render pipeline.
    public class ScratchPipelineAsset : RenderPipelineAsset
    {
        // We don't need any settings yet, so we just need to do one thing: Provide the method that creates an instance
        // of our render pipeline.
        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new ScratchPipeline();
        }
        
        // The following code will only work in the editor, so we add a preprocessor-if to make sure it doesn't try to
        // compile in runtime.
#if UNITY_EDITOR
        // This adds a menu item to the Assets menu in the menu bar, and in the context menu for the Project window.
        [MenuItem("Assets/Create/Rendering/ScratchRP Part 1", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateBasicRenderPipeline()
        {
            // This is a neat utility method to create a new file, and immediately select the file name for editing
            // by the user. The `Action` method below will be called when the user presses enter.
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateScratchPipelineAsset>(),
                "ScratchAsset.asset", null, null);
        }
        
        class CreateScratchPipelineAsset : EndNameEditAction
        {
            // This method is run after the user presses enter when creating a new render pipeline asset.
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                // Create the instance of our pipeline asset.
                var instance = CreateInstance<ScratchPipelineAsset>();
                // Put it into the asset database using the file name the user provided.
                AssetDatabase.CreateAsset(instance, pathName);
            }
        }
#endif
    }
}
