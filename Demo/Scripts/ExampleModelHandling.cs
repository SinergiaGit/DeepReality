using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeepReality.Data;
using DeepReality.Interfaces;
using DeepRealityExample;
using Unity.Barracuda;
using UnityEngine;

namespace DeepReality.Demo.Scripts{
    
    public class ExampleModelHandling : MonoBehaviour, Interfaces.IModelLoader, Interfaces.IPostProcessor, Interfaces.IPreProcessor
    {

        ExampleModelProcessing model;


        [SerializeField]
        NNModel modelFile;
        Model currentModel;


        public List<string> RequiredOutputs => model.RequiredOutputs;

        public int RequiredFrameWidth => model.RequiredFrameWidth;

        public int RequiredFrameHeight => model.RequiredFrameHeight;


        private void Awake()
        {
            model =  new ExampleModelProcessing(typeof(Tensor));
        }


        #region IModelLoader
        public IWorker GetWorker()
        {
            IWorker worker = null;
#if UNITY_IOS //Only IOS
            UnityEngine.Debug.Log("Graphics API: " + SystemInfo.graphicsDeviceType);
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Metal)
            {
                //IOS 11 needed for ARKit, IOS 11 has Metal support only, therefore GPU can run
                var workerType = WorkerFactory.Type.ComputePrecompiled; // GPU
                worker = WorkerFactory.CreateWorker(workerType, currentModel);
            }
            else
            {
                //If Metal support is dropped for some reason, fall back to CPU
                var workerType = WorkerFactory.Type.CSharpBurst;  // CPU
                worker = WorkerFactory.CreateWorker(workerType, currentModel);
            }

#elif UNITY_ANDROID //Only Android
            UnityEngine.Debug.Log("Graphics API: " + SystemInfo.graphicsDeviceType);
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Vulkan)
            {
                //Vulkan on Android supports GPU
                //However, ARCore does not currently support Vulkan, when it does, this line will work
                var workerType = WorkerFactory.Type.ComputePrecompiled; // GPU
                worker = WorkerFactory.CreateWorker(workerType, currentModel);
            }
            else
            {
                //If not vulkan, fall back to CPU
                var workerType = WorkerFactory.Type.CSharpBurst;  // CPU
                worker = WorkerFactory.CreateWorker(workerType, currentModel);
            }
#endif
            return worker;
        }

        public void LoadModel()
        {
            currentModel = ModelLoader.Load(this.modelFile);
        }
        #endregion


        #region IPreProcessor
        public Dictionary<string, Tensor> PreProcess(byte[] textureByteArray)
        {
            Dictionary<string, Tensor> result = new Dictionary<string, Tensor>();

            foreach (var v in model.GetInputTensorData(textureByteArray))
            {
                result[v.TensorName] = new Tensor(v.N, v.Height, v.Width, v.Channels, v.Data);
            }

            return result;
        }
        #endregion


        #region IPostProcessor
        public List<ModelOutput> PostProcess(Dictionary<string, Tensor> tensors)
        {
            Dictionary<string, object> outputs = new Dictionary<string, object>();

            foreach (var t in tensors) outputs[t.Key] = t.Value;

            return model.ExtractOutputData(outputs).Select(
                d => new ModelOutput
                {
                    confidence = d.confidence,
                    data = d.data,
                    description = d.description,
                    screenRect = d.screenRect
                }
            ).ToList();
        }
        #endregion


    }

}
