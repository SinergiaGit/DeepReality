using System.Collections;
using System.Collections.Generic;
using DeepReality.Data;
using DeepReality.Interfaces;
using UnityEngine;

namespace DeepReality.Debugging.Misc{

    /// <summary>
    /// Simple AR Object that shows a text with some info.
    /// </summary>
    public class DebugARObject : MonoBehaviour, IARObject
    {
        public TextMesh textMesh;

        public void UpdateData(ProjectedOutput output)
        {
            textMesh.text = $"{output.description} ({output.confidence})";
        }
    }
    
}
