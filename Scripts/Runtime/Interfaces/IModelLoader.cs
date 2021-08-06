using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;

namespace DeepReality.Interfaces{

    /// <summary>
    /// Interface that needs to be implemented by the class that loads the ML Model.
    /// </summary>
    public interface IModelLoader
    {
        /// <summary>
        /// Called when initializing the ModelExecutor to load the ML Model.
        /// </summary>
        void LoadModel();

        /// <summary>
        /// Called to abtain a worker of the loaded ML Model.
        /// </summary>
        IWorker GetWorker();
    }
    
}
