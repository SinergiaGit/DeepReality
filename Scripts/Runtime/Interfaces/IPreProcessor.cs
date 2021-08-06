using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace DeepReality.Interfaces{

    /// <summary>
    /// Interface that needs to be implemented by the class that prepares the input that will be processed by the ML Model.
    /// </summary>
    public interface IPreProcessor
    {
        /// <summary>
        /// Required width of the image to be prcessed.
        /// </summary>
        public int RequiredFrameWidth { get; }
        /// <summary>
        /// Required height of the image to be processed.
        /// </summary>
        public int RequiredFrameHeight { get; }

        /// <summary>
        /// Convert the byte array of the image to process in all the Tensors that will be used as inputs of the ML Model.
        /// </summary>
        /// <param name="textureByteArray">Byte array of the image to process.</param>
        Dictionary<string, Tensor> PreProcess(byte[] textureByteArray);
    }
    
}
