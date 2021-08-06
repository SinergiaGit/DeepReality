using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeepReality.Utils;
using UnityEngine;

namespace DeepReality.Debugging{

    /// <summary>
    /// Interface that must be implemented by all the MonoBehaviours that want to be able to provide images to the DeepReality session for processing.
    /// </summary>
    public interface IDebugFrameFeeder
    {
        /// <summary>
        /// Method called to retrieve a frame.
        /// </summary>
        /// <param name="requiredWidth">Required width of the output image</param>
        /// <param name="requiredHeight">Required height of the output image</param>
        Task<ImageOperations.ByteArrayTransformationResult> GetFrameAsync(int requiredWidth, int requiredHeight);
    }
    
}
