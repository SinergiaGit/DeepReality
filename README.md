- [DeepReality](#deepreality)
- [Installation](#installation)
  - [Project setup](#project-setup)
- [Basic Usage](#basic-usage)
  - [Scene setup](#scene-setup)
  - [SessionManager](#sessionmanager)
  - [AR Objects](#ar-objects)
- [Creating Model Processing classes](#creating-model-processing-classes)
  - [IModelLoader](#imodelloader)
  - [IPreProcessor](#ipreprocessor)
  - [IPostProcessor](#ipostprocessor)
- [Subscription](#subscription)
- [Debugging](#debugging)
  - [Logging](#logging)
  - [Feeding Frames](#feeding-frames)

# DeepReality
The DeepReality Unity package is aimed at simplifying and streamlining the usage of machine learning models in conjunction with the AR functionality. At its core it links the functionality of:

- **Barracuda**: a neural network inference library
- **AR Foundation**: a package that acts as an interface between Unity and platform-specific AR libraries (ARCore
and ARKit), giving a unified way to access their common functionality

The result is that users with different skill levels in Unity and Machine Learning can easily create mobile applications (iOS and Android) that can:

- Identify objects or features in the world by analyzing via machine learning images acquired with the device’s camera.
- Show content on top of those elements, creating an augmented reality experience.


# Installation
You can import the DeepReality package in your project in mainly two ways:
- From the [Unity Asset Store](https://assetstore.unity.com/packages/slug/201076)
- From the Package Manager using directly the github url


To import the package using the github url you should open the Package Manager inside the Unity Editor and then select "Add package from git URL" (as shown in the image below).\
![alt text](Docs/images/add_package_git_01.png)\
<br/>
It should then open a popup in which to paste the git url.\
![alt text](Docs/images/add_package_git_02.png)\
After entering the url and pressing the "Add" button, the package should be downloaded and imported into your project.\
<br/>
The get url can be found at the top of the Github page.\
![alt text](Docs/images/git_url.png)

## Project setup
Make sure that both AR Foundation and Barracuda packages are imported.\
You can find additional information about installing those packages on their respective documentation:
- [AR Foundation installation guide](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.1/manual/index.html#installing-ar-foundation)
- [Barracuda installation guide](https://docs.unity3d.com/Packages/com.unity.barracuda@2.1/manual/Installing.html)

In your project settings you should also make sure that ARCore and ARKit are enabled in the "XR Plug-in Management" tab.

It should also be noted that currently AR Foundation does not support Vulkan on Android platforms.

Make also sure that in the Player settings the **Api Compatibility Level** is set to **.NET Standard 2.0**\
![alt text](Docs/images/net_standard.png)

# Basic Usage

## Scene setup

The main component that need to be present in the scene is the SessionManager.\
It's responsible of managing the entire DeepReality pipeline and of integrating the various functionalities.\
To easily create a GameObject with this component you can use the appropriate item in the Add menu of the scene.\
![alt text](Docs/images/create_menu.png)

In addtition to the DeepReality SessionManager there needs to be the basic AR Foundation components.\
You can find detailed information on how to setup a scene to work with AR Foundation [here](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.1/manual/index.html#scene-setup)\
The AR Foundation components that are needed are:
- AR Session
- AR Session Origin
- AR Raycast Manager

<br/>You also need to have components that implement the following DeepReality interfaces:
- IModelLoader
- IPreProcessor
- IPostProcessor

These will be used to handle the integration with the ML Model. In the package you can find an implementation that works with the included model.\
For more information check the demo scene.

## SessionManager
The SessionManager'r behaviour can be customized inside the Inspector.\
![alt text](Docs/images/session_inspector.png)

The attributes that can be set are:
- Model execution
  - **autoStart**: Automatically start execution on startup.
  - **modelExecutionInterval**: Minimum time between ML model executions.
- Output management
  - **outputExpirationTime**: Time that an object will remain in the scene after the last time it's recognized.
  - **outputDistanceThreshold**: Minimum distance in world space between recognized objects necessary to consider them distinct objects.
  - **arObjectPrefab**: Prefab to instantiate upon object detection.
- Estimated positions
  - **allowEstimatedPositions**: Allow estimated world space positions if the raycasts through AR Foundation fail.
  - **estimatedPositionDistance**: Distance from the camera of the estimated position.
- Debug
  - **forceDebugFrameFeeder**: Forces the usage of an IDebugFrame feeder on mobile devices (instead of the device's camera).
  - **debugImage**: If set and enabled, the image sent to the ML Model for precessing will be shown here.

If the **autoStart** boolean is not set, the session can be started manually by calling the **StartExecution()** method.
To stop the session you can call the **StopExecution()** method.

## AR Objects

The ultimate purpose of this plugin is to instantiate user specified GameObjects where something was recognized by an object detection ML model. This is all handled by the DeepReality session.

To specify the GameObject you want to instantiate you should set the **arObjectPrefab** attribute of the SessionManager. Whaterver is specified there will be instantiated in the world position where something was recognized. The basic position and rotation are handled automatically.

To have the GameObject respond to data regarding what was actually recognized you should create some components (MonoBehaviours) that implement the **IARObject** interface.

When the **arObjectPrefab** is instantiated, the **UpdateData** method is called on every component that implements the **IARObject** interface. In the **ProjectedOutput** instance that is passed to the method you can find all the data about what was recognized:
- **pose**: World space pose. The object is automatically positioned according to this value, but it can be used to perform any additional actions required.
- **description**: Description string of what was recognized. It should generally be the class label of the recognized object.
- **confidence**: Reported confidence of the recognition.
- **data**: Additional data relevant to the recognition. Its value strongly depends on the ML model (it can also simply be null if not needed).

# Creating Model Processing classes

At the core of the processing of a frame by the ML Model there are 3 different interfaces:
- IModelLoader
- IPreProcessor
- IPostProcessor

In a scene where a DeepReality session should be in execution there must be a component implementing each interface (obviously a component can implement more than one at a time).

The implementation strictly depends on the architecture of the ML Models that you want to use and should be tailored around their needs.

## IModelLoader
```csharp
public interface IModelLoader
{
    // Called when initializing the ModelExecutor to load the ML Model.
    void LoadModel();

    // Called to abtain a worker of the loaded ML Model.
    IWorker GetWorker();
}
```

## IPreProcessor
```csharp
public interface IPreProcessor
{
    // Required width of the image to be prcessed.
    public int RequiredFrameWidth { get; }
    // Required height of the image to be processed.
    public int RequiredFrameHeight { get; }

    // Convert the byte array of the image to process in all the Tensors that will be used as inputs of the ML Model.
    Dictionary<string, Tensor> PreProcess(byte[] textureByteArray);
}
```
## IPostProcessor
```csharp
public interface IPostProcessor
{
    // Names of the Tensors that are required to process the output.
    List<string> RequiredOutputs { get; }

    // Processes the outputs coming from the ML Model's execution and transforms the in a list of "ModelOutput".
    // Each "ModelOutput" returned represents an object that was recognized, with all the relevant data.
    /// Takes as input the utput tensors containing the results of the ML Model execution.</param>
    List<ModelOutput> PostProcess(Dictionary<string,Tensor> tensors);
}
```
# Subscription
**NOTE: The features described in this paragraph are not available in the package found in the Unity Asset Store.**

If you are a subcriber of the [DeepReality platform](https://www.deepreality.it/) you can use the **SubscriptionModelProcessing** class to download a ML model directly into your project and be able to use it in the DeepReality pipeline without further coding.

This component implements all the 3 required interfaces described in the previous paragraph, so it handles all the processing required to execute succesfully the model and retrieve the appropriate outputs.

To easily create a GameObject with this component you can use the appropriate item in the Add menu of the scene. \
Once you have the component in your scene you con configure it using the Inspector. 

The first thing to do is to insert the Subscription Key obtained through the DeepReality portal.\
![alt text](Docs/images/subscription_inspector_01.png)\
Once it's entered you can press the "DOWNLOAD DATA" button. This will take care of downloading the ML Model (in ONNX format as required by barracuda) and importing it into your project. It also retrieves any additional information.\
If the process is succesfull you should no longer see the error message and a list of available classes to recognize should appear.\
![alt text](Docs/images/subscription_inspector_02.png)\
You can enable and disable single classes. If a class is disabled it will not be recognized in the analyzed frames.\
You can also use the buttons at the bottom of the list to enable or disable all classes.\
![alt text](Docs/images/subscription_inspector_03.png)

Once you are done configuring the component and save the scene, the DeepReality session is good to go!

# Debugging

## Logging

If you want to access diagnostic data about the DeepReality session you can create components that implement the **ILogger** interface.

The DeepReality session automatically searches for any ILogger present in the scene and sends them data after each recognition. This is done through an instance of the **LogData** class. This class contains:

- **modelProcessingTime**: Time it took to execute the ML Model with the current frame (in ms)
- **arProjectionTime**: Time it took to project ModelOutputs in world space (in ms)
- **arRaycastsHit**: Number of succesful raycasts performed by the ARProjector.
- **arRaycastsTotal**: Total number of raycasts performed by the ARProjector.
- **modelOutputs**: List of all the ModelOutputs of the current frame.
- **projectedOutputs**: List of all the ProjectedOutputs of the current frame.

Some implementations of the **ILogger** interface con be found in the demo scene.

## Feeding Frames
If for debugging purposes you want to use custom images instead of the camera feed for the DeepReality pipeline you can create a component that implements the **IDebugFrameFeeder** interface.

The DeepReality session searches at startup for an **IDebugFrameFeeder**.\
If the project is running in the editor it will be used instead of the camera feed that would normally be coming from AR Foundation.\
If the project is running on a mobile device the frame feeder will instead be disabled in favor of the camera feed. If you want to use the frame feeder even on a mobile build you can do so by setting the **forceDebugFrameFeeder** boolean on the DeepReality **SessionManager**.

The **IDebugFrameFeeder** interface consists of a single method: **GetFrameAsync(int requiredWidth, int requiredHeight)**. The implementation should return an istance of **ByteArrayTransformationResult** (wrapped inside a Task as the method should be asyncronous).

The **ByteArrayTransformationResult** class contains:
- **byteArray**: the byte array of the RGB image already rescaled in the size required. The required size is passed as parameters to the method.
- **originalAspect**: the original aspect ratio of the image.
- **newAspect**: the aspect ratio of the image contained in the byte array.

The aspect ratios are used to perform calculations on the eventual areas of the image that the ML model recognizes as something of interest.

Some implementations of the **IDebugFrameFeeder** interface con be found in the demo scene.