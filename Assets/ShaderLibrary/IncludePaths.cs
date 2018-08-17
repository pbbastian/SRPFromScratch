// DO NOT DELETE THIS FILE
// Otherwise the Scratch shader library won't work.
using System.IO;
using UnityEditor;

namespace ShaderLibrary
{
    public class IncludePaths
    {
        [ShaderIncludePath]
        static string[] GetShaderIncludePaths()
        {
            return new[] { Path.GetFullPath("Assets/ShaderLibrary") };
        }
    }
}