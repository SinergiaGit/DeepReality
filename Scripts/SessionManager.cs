using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepReality.ModelHandling;
using DeepReality.ARProjection;
using DeepReality.Session;
using DeepReality.Data;
using System.Threading.Tasks;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using System;
using DeepReality.Utils;
using static DeepReality.Utils.ImageOperations;

namespace DeepReality{

    /// <summary>
    /// Orchestrator for the whole DeepReality pipeline.
    /// Manages the communication between the various parts of the plugin.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        /// <summary>
        /// Current active instance.
        /// </summary>
        public static SessionManager Instance { get; protected set; }

        /// <summary>
        /// If true, the session starts automatically upon scene startup.
        /// Otherwise it will need to be started manually by calling the "StartExecution" method.
        /// </summary>
        [Header("Model execution")]
        [Tooltip("Automatically start execution on startup.")]
        public bool autoStart = true;
        /// <summary>
        /// Minimum time between ML model executions.
        /// </summary>
        [Tooltip("Minimum time between ML model executions.")]
        public float modelExecutionInterval = 1f;

        /// <summary>
        /// Time that an object will remain in the scene after the last time it's recognized.
        /// </summary>
        [Header("Output management")]
        [Tooltip("Time that an object will remain in the scene after the last time it's recognized.")]
        public float outputExpirationTime = 5f;
        /// <summary>
        /// Minimum distance in world space between recognized objects necessary to consider them distinct objects.
        /// </summary>
        [Tooltip("Minimum distance in world space between recognized objects necessary to consider them distinct objects.")]
        public float outputDistanceThreshold = 0.5f;
        /// <summary>
        /// Prefab to instantiate upon object detection.
        /// </summary>
        [Tooltip("Prefab to instantiate upon object detection.")]
        public GameObject arObjectPrefab;

        /// <summary>
        /// Allow estimated world space positions if the raycasts through AR Foundation fail.
        /// </summary>
        [Header("Estimated positions")]
        [Tooltip("Allow estimated world space positions if the raycasts through AR Foundation fail.")]
        public bool allowEstimatedPositions = true;
        /// <summary>
        /// Distance from the camera of the estimated position.
        /// </summary>
        [Tooltip("Distance from the camera of the estimated position.")]
        public float estimatedPositionDistance = 1f;

        /// <summary>
        /// Forces the usage of an IDebugFrame feeder on mobile devices (instead of the device's camera).
        /// </summary>
        [Header("Debug")]
        [Tooltip("Forces the usage of an IDebugFrame feeder on mobile devices (instead of the device's camera).")]
        public bool forceDebugFrameFeeder;
        /// <summary>
        /// If set and enabled, the image sent to the ML Model for precessing will be shown here.
        /// </summary>
        [Tooltip("If set and enabled, the image sent to the ML Model for precessing will be shown here.")]
        public RawImage debugImage;

        /// <summary>
        /// Texture used with the "debugImage"
        /// </summary>
        Texture2D debugTexture;

        /// <summary>
        /// Reference to the current "ModelExecutor" instance.
        /// </summary>
        ModelExecutor modelExecutor;
        /// <summary>
        /// Reference to the current "ARProjector" instance.
        /// </summary>
        ARProjector arProjector;
        /// <summary>
        /// Reference to an AR Foundation's "ARCameraManager" found in the scene. Used to get images from the camera.
        /// </summary>
        ARCameraManager cameraManager;
        
        /// <summary>
        /// Reference to the current "SessionOutputList" instance.
        /// </summary>
        public SessionOutputList SessionOutputList { get; protected set; }

        /// <summary>
        /// Set to true one the MonoBehaviour is destroyed.
        /// </summary>
        bool isDestroyed = false;
        /// <summary>
        /// Se to true if the session is started.
        /// </summary>
        bool isStarted = false;
        /// <summary>
        /// Calculates if the sessione is in execution.
        /// </summary>
        bool isExecuting => !isDestroyed && isStarted;

        
        /// <summary>
        /// Current instance of an "IDebugFrameFeeder" (if any).
        /// Used to get frames wihtout using the camera.
        /// </summary>
        Debugging.IDebugFrameFeeder frameFeeder;

        /// <summary>
        /// All instances of "ILogger" found in the scene.
        /// They will receive debug information.
        /// </summary>
        List<Debugging.ILogger> loggers;
        /// <summary>
        /// True if there are any "ILogger" instances that will receive debug data.
        /// </summary>
        bool isLogging = false;

        /// <summary>
        /// DisplayMatrix retrieved from AR Foundation. Used to calculate the camera frame rotation.
        /// </summary>
        Matrix4x4? lastDisplayMatrix;


        /// <summary>
        /// Set the static instance after the MonoBehaviour instantiated.
        /// </summary>
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this.gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Start the initialitazion.
        /// If "autoStart" is set, starts the execution.
        /// </summary>
        private void Start()
        {
            Init();
            if(autoStart) StartExecution();
        }


        /// <summary>
        /// Finds all the required components in the scene.
        /// Instantiates any additional necessary classes.
        /// </summary>
        void Init()
        {
            var components = FindObjectsOfType<MonoBehaviour>().Where(c => c.isActiveAndEnabled);

            var modelLoader = components.OfType<Interfaces.IModelLoader>().FirstOrDefault();
            if (modelLoader == null) throw new System.Exception("IModelLoader implementation not found in the scene.");

            var preProcessor = components.OfType<Interfaces.IPreProcessor>().FirstOrDefault();
            if (preProcessor == null) throw new System.Exception("IPreProcessor implementation not found in the scene.");

            var postProcessor = components.OfType<Interfaces.IPostProcessor>().FirstOrDefault();
            if (postProcessor == null) throw new System.Exception("IPostProcessor implementation not found in the scene.");

            cameraManager = components.OfType<ARCameraManager>().FirstOrDefault();
            if (cameraManager == null) throw new System.Exception("ARCameraManager not found in the scene.");
            cameraManager.frameReceived += OnCameraFrameReceived;

            var raycastManager = components.OfType<ARRaycastManager>().FirstOrDefault();
            if (raycastManager == null) throw new System.Exception("ARRaycastManager implementation not found in the scene.");

            var arSessionOrigin = components.OfType<ARSessionOrigin>().FirstOrDefault();
            if (arSessionOrigin == null) throw new System.Exception("ARRaycastManager implementation not found in the scene.");

            modelExecutor = new ModelExecutor(modelLoader, preProcessor, postProcessor);
            arProjector = new ARProjector(raycastManager, arSessionOrigin);
            SessionOutputList = new SessionOutputList(this);


            frameFeeder = components.OfType<Debugging.IDebugFrameFeeder>().FirstOrDefault();
            if (!Application.isEditor && !forceDebugFrameFeeder && frameFeeder != null)
            {
                if(frameFeeder is MonoBehaviour ffmb)
                {
                    Destroy(ffmb.gameObject);
                }
                frameFeeder = null;
            }

            loggers = components.OfType<Debugging.ILogger>().ToList();
            isLogging = loggers != null && loggers.Count > 0;
        }

        /// <summary>
        /// "ARCameraManager" frameReceived event hanlder.
        /// Gets the current Display Matrix.
        /// </summary>
        /// <param name="frameEventArgs">Event data</param>
        private void OnCameraFrameReceived(ARCameraFrameEventArgs frameEventArgs)
        {
            lastDisplayMatrix = frameEventArgs.displayMatrix;
        }

        /// <summary>
        /// Perform some cleanup before destruction.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            isDestroyed = true;
            cameraManager.frameReceived -= OnCameraFrameReceived;
        }

        /// <summary>
        /// Start the session execution.
        /// </summary>
        public void StartExecution()
        {
            isStarted = true;
            MainLoop();
        }
        /// <summary>
        /// Stop the session execution.
        /// </summary>
        public void StopExecution()
        {
            isStarted = false;
        }

        /// <summary>
        /// Main Execution loop.
        /// Continues execution indefinitely unitil stopped.
        /// </summary>
        async void MainLoop()
        {
            while (isExecuting)
            {
                //Wait until the last frame finished processing and the "modelExecutionInterval" has passed
                await Task.WhenAll(
                    ProcessFrameAsync(),
                    Task.Delay((int)(modelExecutionInterval * 1000f))
                    );
            }
        }

        
        
        /// <summary>
        /// Performs the whole DeepReality pipeline on a single frame.
        /// </summary>
        async Task ProcessFrameAsync()
        {
            try
            {
                //Get the texture to process
                var textureByteArray = await GetCameraTexture();

                if (textureByteArray == null) return;


                Debugging.LogData logData = new Debugging.LogData();

                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

                stopwatch.Start();

                //Process the frame through the ML Model
                var outputs = await modelExecutor.ProcessFrameAsync(textureByteArray.byteArray);

                stopwatch.Stop();
                logData.modelProcessingTime = stopwatch.ElapsedMilliseconds;

                //Adjust the screen rects of the outputs frome the space of the processed image to the space of the image visible on the screen.
                float screenAspect = (float)Screen.width / (float)Screen.height;
                foreach (var o in outputs)
                {
                    o.AdjustRect(textureByteArray.originalAspect, textureByteArray.newAspect, screenAspect);
                }

                stopwatch.Reset();
                stopwatch.Start();

                //Project the "ModelOutputs" to get world space coordinates.
                var projected = outputs.Select(o => arProjector.RaycastOutput(o, logData)).ToList();

                stopwatch.Stop();
                logData.arProjectionTime = stopwatch.ElapsedMilliseconds;

                logData.modelOutputs = outputs;
                logData.projectedOutputs = projected;

                if (isLogging) loggers.ForEach(l => l.Log(logData));

                //Update the "SessionOutputList" to hanle AR Objects instantiation.
                SessionOutputList.UpdateSession(projected);

                if(debugImage!=null && debugImage.isActiveAndEnabled)
                {
                    if (debugTexture != null) Destroy(debugTexture);
                    debugTexture = new Texture2D(modelExecutor.RequiredFrameWidth, modelExecutor.RequiredFrameHeight, TextureFormat.RGB24, false);
                    debugTexture.LoadRawTextureData(textureByteArray.byteArray);
                    debugTexture.Apply();
                    debugImage.texture = debugTexture;
                }

            }catch(Exception e)
            {
                UnityEngine.Debug.LogWarning(e.ToString());
                UnityEngine.Debug.LogWarning(e.StackTrace);
            }
            
            

        }

        /// <summary>
        /// Get the texture to be processed.
        /// Normally retrieved from the camera through AR Foundation.
        /// Optionally can use an "IDebugFrameFeeder" instance.
        /// </summary>
        async Task<ByteArrayTransformationResult> GetCameraTexture()
        {
            if(frameFeeder != null) return await frameFeeder.GetFrameAsync(modelExecutor.RequiredFrameWidth, modelExecutor.RequiredFrameHeight);

#if !UNITY_EDITOR
            if (lastDisplayMatrix == null) return null;
            if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage img))
            {
                try
                {
                    //Transform the XrCpuImage coming from AR Foundation to a byte array with the corect size and the required additional information.
                    return await img.ToTransformedByteArray(modelExecutor.RequiredFrameWidth, modelExecutor.RequiredFrameHeight, lastDisplayMatrix.Value);
                }
                catch
                {
                    return null;
                }
                finally
                {
                    img.Dispose();
                }
            }
#endif

            return null;
        }
    }
}
