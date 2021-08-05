using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeepReality.Data{

    /// <summary>
    /// Output of the ML Models converted in world space.
    /// </summary>
    [System.Serializable]
    public class ProjectedOutput
    {
        /// <summary>
        /// World space pose.
        /// </summary>
        public Pose pose;
        /// <summary>
        /// Description string of what was recognized.
        /// </summary>
        public string description;
        /// <summary>
        /// Additional data relevant to the recognition.
        /// </summary>
        public object data;
        /// <summary>
        /// Reported confidence of the recognition.
        /// </summary>
        public float confidence;
    }
    
}
