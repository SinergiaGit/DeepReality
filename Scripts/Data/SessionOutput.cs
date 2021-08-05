using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeepReality.Data{

    /// <summary>
    /// A ProjectedOutput with additional data regarding its lifecycle and usage inside the DeepReality session.
    /// </summary>
    [System.Serializable]
    public class SessionOutput
    {
        /// <summary>
        /// Associated projected output.
        /// </summary>
        public ProjectedOutput output;
        /// <summary>
        /// Last time the output was detected by the ML Model.
        /// </summary>
        public float lastDetection;
        /// <summary>
        /// Anchor of the AR Object. Placed according to the output's world space pose.
        /// </summary>
        public Transform anchor;
        /// <summary>
        /// Game object used to present the AR content. Child of the anchor.
        /// </summary>
        public GameObject arObject;


    }
    
}
