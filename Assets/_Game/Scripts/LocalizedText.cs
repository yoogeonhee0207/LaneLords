using UnityEngine;
using UnityEngine.UI;

namespace TinySwordsBattle
{
    /// <summary>Puts the right language into a fixed label when the scene starts.</summary>
    public class LocalizedText : MonoBehaviour
    {
        public string english;

        void Awake()
        {
            var t = GetComponent<Text>();
            if (t != null) t.text = Loc.Static(english);
        }
    }
}
