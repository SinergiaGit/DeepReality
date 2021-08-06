using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeepReality.Data;
using DeepReality.Interfaces;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using DeepReality.Utils;

namespace DeepReality.ModelHandling{

    /// <summary>
    /// 
    /// </summary>
    public class ModelExecutor
    {
        /// <summary>
        /// "IModelLoader" instance used to load the model and get the workers.
        /// </summary>
        IModelLoader modelLoader;
        /// <summary>
        /// "IPreProcessor" instance used to prepared the input image for the ML Model.
        /// </summary>
        IPreProcessor preProcessor;
        /// <summary>
        /// "IPostPorcessor" instance used to get the outputs of the ML Model.
        /// </summary>
        IPostProcessor postProcessor;
        /// <summary>
        /// Worker used to execute the model.
        /// </summary>
        IWorker worker;

        /// <summary>
        /// Required width of the image to be prcessed.
        /// </summary>
        public int RequiredFrameWidth => preProcessor.RequiredFrameWidth;
        /// <summary>
        /// Required height of the image to be prcessed.
        /// </summary>
        public int RequiredFrameHeight => preProcessor.RequiredFrameHeight;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="modelLoader">"IModelLoader" instance to use.</param>
        /// <param name="preProcessor">"IPreProcessor" instance to use.</param>
        /// <param name="postProcessor">"IPostProcessor" instance to use.</param>
        public ModelExecutor(IModelLoader modelLoader, IPreProcessor preProcessor, IPostProcessor postProcessor)
        {
            this.modelLoader = modelLoader;
            this.preProcessor = preProcessor;
            this.postProcessor = postProcessor;

            this.modelLoader.LoadModel();

            worker = modelLoader.GetWorker();
        }

        /// <summary>
        /// Dictionary of tensors used to put the tensors containing the output of the ML Model.
        /// </summary>
        Dictionary<string, Tensor> outputTensors = new Dictionary<string, Tensor>();

        /// <summary>
        /// Execute the whole model execution pipeline on an image passed as byte array.
        /// </summary>
        /// <param name="textureByteArray">Byte Array of the image to process.</param>
        public async Task<List<ModelOutput>> ProcessFrameAsync(byte[] textureByteArray)
        {
            //Ensure that the method is in execution on the main thread.
            await AsyncUtils.WaitForMainThreadAsync();

            //Pre process the image and get the input tensors.
            var tensors = preProcessor.PreProcess(textureByteArray);
            //Execute the ML Model with the calculated inputs.
            await worker.StartManualSchedule(tensors).ToTask();

            //Get the output tensors.
            outputTensors.Clear();
            foreach(var t in postProcessor.RequiredOutputs)
            {
                outputTensors[t] = worker.PeekOutput(t);
            }

            //Post process the output tensors to calculate the appropriate ModelOutputs.
            var res = postProcessor.PostProcess(outputTensors);

            //Dispose the various tensors used.
            DisposeTensorDictionary(tensors);
            DisposeTensorDictionary(outputTensors);

            return res;
        }

        /// <summary>
        /// Dispose all the tensors in a dictionary.
        /// </summary>
        /// <param name="dict">Dictionary of tensors to dispose.</param>
        void DisposeTensorDictionary(Dictionary<string, Tensor> dict)
        {
            if (dict == null) return;
            foreach(var kv in dict)
            {
                kv.Value.Dispose();
            }
        }
    }
    
}
