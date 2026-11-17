using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace TinySwordsBattle.EditorTools
{
    /// <summary>
    /// Turns the CRAZY_SDK define on when the CrazyGames SDK folder is in the project (and off when it is not),
    /// so Ads.cs talks to the real SDK only when it is there.
    /// </summary>
    [InitializeOnLoad]
    static class CrazySdkDefine
    {
        const string Symbol = "CRAZY_SDK";

        static CrazySdkDefine()
        {
            bool present = Directory.Exists("Assets/CrazySDK");
            foreach (var target in new[] { NamedBuildTarget.WebGL, NamedBuildTarget.Standalone })
            {
                var symbols = PlayerSettings.GetScriptingDefineSymbols(target)
                    .Split(';').Where(s => s.Length > 0).ToList();
                bool has = symbols.Contains(Symbol);
                if (present == has) continue;
                if (present) symbols.Add(Symbol); else symbols.Remove(Symbol);
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols));
            }
        }
    }
}
