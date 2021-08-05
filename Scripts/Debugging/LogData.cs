using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeepReality.Debugging{

    /// <summary>
    /// Class used to store diagnostic data for the logs
    /// </summary>
    public class LogData 
    {
        /// <summary>
        /// Time it took to execute the ML Model with the current frame (in ms)
        /// </summary>
        public long modelProcessingTime;
        /// <summary>
        /// Time it took to project ModelOutputs in world space (in ms)
        /// </summary>
        public long arProjectionTime;
        /// <summary>
        /// Number of succesful raycasts performed by the ARProjector.
        /// </summary>
        public int arRaycastsHit = 0;
        /// <summary>
        /// Total number of raycasts performed by the ARProjector.
        /// </summary>
        public int arRaycastsTotal = 0;
        /// <summary>
        /// List of all the ModelOutputs of the current frame.
        /// </summary>
        public List<Data.ModelOutput> modelOutputs;
        /// <summary>
        /// List of all the ProjectedOutputs of the current frame.
        /// </summary>
        public List<Data.ProjectedOutput> projectedOutputs;
    }
    
}
