using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeepReality.Data;
using DeepReality.Debugging;
using DeepReality.Utils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace DeepReality.ARProjection{

    /// <summary>
    /// Class responsible of transforming a ModelOutput to a ProjectedOutput.
    /// </summary>
    public class ARProjector
    {
        /// <summary>
        /// ARRaycastManager used to perform raycasts.
        /// </summary>
        ARRaycastManager raycastManager;
        /// <summary>
        /// ARSessionOrigin used to find the AR camera.
        /// </summary>
        ARSessionOrigin sessionOrigin;
        /// <summary>
        /// List of results of the raycasting operations.
        /// </summary>
        List<ARRaycastHit> hitResults;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="raycastManager">ARRaycastManager used in the scene.</param>
        /// <param name="sessionOrigin">ARSessionOrigin used in the scene.</param>
        public ARProjector(ARRaycastManager raycastManager, ARSessionOrigin sessionOrigin)
        {
            this.raycastManager = raycastManager;
            this.sessionOrigin = sessionOrigin;
            hitResults = new List<ARRaycastHit>();
        }

        /// <summary>
        /// Raycasts a ModelOutput to get its correspondig ProjectedOutput.
        /// </summary>
        /// <param name="output">ModelOutput coming from the ML Model processing.</param>
        /// <param name="logData">LogData in which to append processing information. (optional)</param>
        public ProjectedOutput RaycastOutput(ModelOutput output, LogData logData =null)
        {
            //Get the world space pose of something recognized by the ML model
            var result = GetRaycastedOutput(output, logData);

            
            if(result!=null)
            {
                //Make the world pose's rotation look at the camera
                Vector3 faceDirection =  sessionOrigin.camera.transform.position - result.pose.position;
                faceDirection.y = 0;
                faceDirection.z = -faceDirection.z;
                faceDirection.x = -faceDirection.x;
                faceDirection.Normalize();
                result.pose.rotation = Quaternion.LookRotation(faceDirection);
            }

            return result;
        }

        /// <summary>
        /// Actual method used to perform the raycasting and world space conversion.
        /// </summary>
        /// <param name="output">ModelOutput coming from the ML Model processing.</param>
        /// <param name="logData">LogData in which to append processing information. (optional)</param>
        ProjectedOutput GetRaycastedOutput(ModelOutput output, LogData logData = null)
        {
#if UNITY_EDITOR
            //If inside the Editor AR Foundation is not available
            //Instead of by raycasting, the world pose is estimated

            var screenPos = output.GetDenormalizedScreenRect(Screen.width, Screen.height).center;
            Vector3 worldPos = sessionOrigin.camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 2f));
            return new ProjectedOutput
            {
                data = output.data,
                description = output.description,
                pose = new Pose(worldPos,Quaternion.identity),
                confidence = output.confidence
            };
#else

            //Try to get a world space pose through raycasting
            var averagedProjectedPose = GetAveragedPose(output, logData);

            //If a world space pose if found it's returned alongised all additional recognition data
            if (averagedProjectedPose!=null)
            {
                return new ProjectedOutput
                {
                    data = output.data,
                    description = output.description,
                    pose = averagedProjectedPose.Value,
                    confidence = output.confidence
                };
            }
            //Otherwise an estimated pose is returned (if allowed in the SessionManager)
            else if (DeepReality.SessionManager.Instance.allowEstimatedPositions) {
                var screenPos = output.GetDenormalizedScreenRect(Screen.width, Screen.height).center;
                Vector3 worldPos = sessionOrigin.camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, DeepReality.SessionManager.Instance.estimatedPositionDistance));
                return new ProjectedOutput
                {
                    data = output.data,
                    description = output.description,
                    pose = new Pose(worldPos, Quaternion.identity),
                    confidence = output.confidence
                };
            }

            return null;
#endif
        }

        /// <summary>
        /// Get the screen space positions in which to perform the raycasts.
        /// </summary>
        /// <param name="output">ModelOutput coming from the ML Model processing.</param>
        /// <param name="intermediateSteps">Number of intermiade positions to calculate between the center and the perimeter.</param>
        /// <param name="cornersPercentage">Percentage of the size of the rect to use for calculations.</param>
        List<Vector2> GetRaycastPositions(ModelOutput output, int intermediateSteps, float cornersPercentage)
        {
            //Get the screen space rect of the recognized area
            Rect screenRect = output.GetDenormalizedScreenRect(Screen.width, Screen.height);

            //Calculate the center of the rect
            var center = screenRect.center;

            //Calculate points around the perimeter of the rect (reduced to "cornersPercentage" of its original size)
            var corners = new Vector2[]
            {
                Vector2.Lerp(center, new Vector2(screenRect.xMin, screenRect.yMin),cornersPercentage),
                Vector2.Lerp(center,new Vector2(screenRect.xMin, screenRect.yMax),cornersPercentage),
                Vector2.Lerp(center,new Vector2(screenRect.xMax, screenRect.yMax),cornersPercentage),
                Vector2.Lerp(center,new Vector2(screenRect.xMax, screenRect.yMin),cornersPercentage),
                Vector2.Lerp(center,new Vector2(screenRect.xMin, screenRect.center.y),cornersPercentage),
                Vector2.Lerp(center,new Vector2(screenRect.xMax, screenRect.center.y),cornersPercentage),
                Vector2.Lerp(center,new Vector2(screenRect.center.x, screenRect.yMin),cornersPercentage),
                Vector2.Lerp(center,new Vector2(screenRect.center.x, screenRect.yMax),cornersPercentage)
            };
                

            var result = new List<Vector2>
            {
                center
            };

            result.AddRange(corners);

            //Calculate more points inside the rect
            float step = 1f / (float)(intermediateSteps+1);
            for(int i = 0; i < intermediateSteps; i++)
            {
                result.AddRange(corners.Select(c => Vector2.Lerp(center, c, step * i)));
            }

            //Return a list of all the points that will be used to perform raycasts
            //More than 1 point is returned to increase the probability of actually hitting something in the recongized area
            return result;
        }

        /// <summary>
        /// Get the poses associated to every succesful raycast.
        /// </summary>
        /// <param name="output">ModelOutput coming from the ML Model processing.</param>
        /// <param name="logData">LogData in which to append processing information. (optional)</param>
        List<Pose> GetRaycastedPoses(ModelOutput output, LogData logData = null)
        {
            //Get the screen space points used to perform raycasts
            var positions = GetRaycastPositions(output,2,0.5f);
            List<Pose> resultPoses = new List<Pose>();

            logData.arRaycastsTotal = positions.Count;

            //Try to raycast each point and get the resulting world space poses
            foreach (var p in positions)
            {
                if (raycastManager.Raycast(p, hitResults, UnityEngine.XR.ARSubsystems.TrackableType.Planes | UnityEngine.XR.ARSubsystems.TrackableType.FeaturePoint))
                {
                    resultPoses.Add(hitResults[0].pose);
                }
            }

            logData.arRaycastsHit = resultPoses.Count;

            return resultPoses;
        }

        /// <summary>
        /// Get the pose calculated by averaging the pose of every succesful raycast.
        /// </summary>
        /// <param name="output">ModelOutput coming from the ML Model processing.</param>
        /// <param name="logData">LogData in which to append processing information. (optional)</param>
        Pose? GetAveragedPose(ModelOutput output, LogData logData = null)
        {
            //Get a list of worldspace poses associated to various points inside the recongized area
            var poses = GetRaycastedPoses(output, logData);

            //If no poses are present it means that all the raycasts have failed (and no world space pose has been found)
            if (poses == null || poses.Count == 0) return null;

            //Get an average position and rotation from all the poses
            Vector3 averagedPosition = poses.Select(p => p.position).Average();
            Quaternion averagedRotation = poses.Select(p => p.rotation).Average();

            //Return a pose made from the averaged values
            return new Pose(averagedPosition, averagedRotation);
        }
    }
    
}
