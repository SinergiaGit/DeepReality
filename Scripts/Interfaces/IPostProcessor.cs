using System.Collections;
using System.Collections.Generic;
using DeepReality.Data;
using Unity.Barracuda;
using UnityEngine;

namespace DeepReality.Interfaces{

    /// <summary>
    /// Interface that needs to be implemented by the class that processes the output coming from the ML Model.
    /// </summary>
    public interface IPostProcessor
    {
        /// <summary>
        /// Names of the Tensors that are required to process the output.
        /// </summary>
        List<string> RequiredOutputs { get; }

        /// <summary>
        /// Processes the outputs coming from the ML Model's execution and transforms the in a list of "ModelOutput".
        /// Each "ModelOutput" returned represents an object that was recognized, with all the relevant data.
        /// </summary>
        /// <param name="tensors">Output tensors containing the results of the ML Model execution.</param>
        List<ModelOutput> PostProcess(Dictionary<string,Tensor> tensors);
    }
    
}
