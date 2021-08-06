using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DeepReality.Utils.Editor{
    
    public class MenuItems
    {
        [MenuItem("GameObject/DeepReality/DeepReality Session")]
        private static void AddDeepRealitySession(MenuCommand menuCommand)
        {
            // Create a custom game object
            GameObject go = new GameObject("DeepRealitySession");
            SessionManager sm = go.AddComponent<SessionManager>();
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }
    }
    
}
