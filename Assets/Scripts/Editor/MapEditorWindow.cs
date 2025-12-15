using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Gameplay.Config {
    /// <summary>
    /// Editor window for setting up, saving, and loading maps. Intended to be used for serializing/deserializing grid
    /// state in the game scene.  
    /// </summary>
    [InitializeOnLoad]
    public class MapEditorWindow : OdinEditorWindow {
        static MapEditorWindow() {
            // Ran whenever recompiling the project
            EditorApplication.delayCall += () => {
                Instance.SetUpWindow();
            };
        }

        private static MapEditorWindow _instance;
        private static MapEditorWindow Instance {
            get {
                if (_instance == null) {
                    _instance = GetWindow<MapEditorWindow>();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// The '%m' allows for the keyboard shortcut CTRL + M for opening the window. 
        /// </summary>
        [MenuItem("Tools/Scene Navigation %m")]
        private static void OpenWindow() {
            Instance.SetUpWindow();
            Instance.Show();
        }

        private void SetUpWindow() {
            
        }
    }
}