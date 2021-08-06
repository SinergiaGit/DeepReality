using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeepReality.Utils;
using UnityEngine;

namespace DeepReality.Session{

    /// <summary>
    /// Class that hadles the lifecycle of all the AR Objects that are instantiated during a DeepReality session.
    /// </summary>
    public class SessionOutputList
    {
        /// <summary>
        /// Event called when a new output is recognized (and a new AR Object instantiated).
        /// </summary>
        public event Action<Data.SessionOutput> OnOutputAdded;
        /// <summary>
        /// Event called when an output expires and is removed.
        /// </summary>
        public event Action<Data.SessionOutput> OnOutputRemoved;
        /// <summary>
        /// Event called when an output is updated.
        /// </summary>
        public event Action<Data.SessionOutput> OnOutputUpdated;

        /// <summary>
        /// Time that an object will remain in the scene after the last time it's recognized.
        /// </summary>
        float expirationTime => sessionManager.outputExpirationTime;
        /// <summary>
        /// Prefab to instantiate when a new output is added.
        /// </summary>
        GameObject outputPrefab => sessionManager.arObjectPrefab;
        /// <summary>
        /// Minimum distance in world space between recognized objects necessary to consider them distinct objects.
        /// </summary>
        float distanceTrheshold => sessionManager.outputDistanceThreshold;

        /// <summary>
        /// Current "SessionManager" instance.
        /// </summary>
        SessionManager sessionManager;

        /// <summary>
        /// List of SessionOutputs currently active.
        /// </summary>
        List<Data.SessionOutput> CurrentOutputs = new List<Data.SessionOutput>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sessionManager">Current SessionManager instance.</param>
        public SessionOutputList(SessionManager sessionManager)
        {
            this.sessionManager = sessionManager;
        }


        /// <summary>
        /// Takes the list of ProjectedOutputs that are detected in a frame and updates the SessionOutputs accordingly.
        /// New AR Objects could be instantiated or old ones could be updated or removed.
        /// </summary>
        /// <param name="projectedOutputs">ProjectedOutputs detected this frame.</param>
        public void UpdateSession(List<Data.ProjectedOutput> projectedOutputs)
        {
            if (outputPrefab == null) return;

            if (projectedOutputs == null) projectedOutputs = new List<Data.ProjectedOutput>();

            projectedOutputs = projectedOutputs.Where(o => o != null).ToList();

            Data.SessionOutput previous = null;

            foreach (var o in projectedOutputs)
            {
                previous = CurrentOutputs.FirstOrDefault(c => IsSameArea(o, c.output));
                if(previous!=null)
                {
                    UpdateOutput(previous, o);
                    OnOutputUpdated?.Invoke(previous);
                    continue;
                }

                AddOutput(o);
            }

            var expiredOutputs = CurrentOutputs.Where(o => IsOutputExpired(o)).ToList();
            foreach(var e in expiredOutputs)
            {
                RemoveOutput(e);
            }
        }

        /// <summary>
        /// Determines if two ProjectedOutputs are approximately in the same world space position.
        /// Used to decide if an output is new or if an already existing one should be updated.
        /// </summary>
        /// <param name="a">First ProjectedOutput</param>
        /// <param name="b">Second ProjectedOutput</param>
        protected bool IsSameArea(Data.ProjectedOutput a, Data.ProjectedOutput b)
        {
            float dst = Vector3.Distance(a.pose.position, b.pose.position);

            if (a.description == b.description) dst /= 2;

            if (dst <= distanceTrheshold) return true;

            return false;
        }

        /// <summary>
        /// Calculates if a certain SessionOutput is expired.
        /// </summary>
        /// <param name="output">SessionOutput to check.</param>
        protected bool IsOutputExpired(Data.SessionOutput output) => (Time.time - output.lastDetection) >= expirationTime;

        /// <summary>
        /// Update the data of a SessionOutput with the one coming from a ProjectedOutput.
        /// </summary>
        /// <param name="current">SessionOutput to update.</param>
        /// <param name="updated">ProjectedOutput containing the new data to use.</param>
        /// <param name="animated">Should the translation to the new world space position be animated?</param>
        protected void UpdateOutput(Data.SessionOutput current, Data.ProjectedOutput updated, bool animated=true)
        {
            current.lastDetection = Time.time;
            current.output = updated;
            current.arObject.GetComponent<Interfaces.IARObject>()?.UpdateData(current.output);

            if(animated)
                SmoothUpdatePose(current);
            else
            {
                current.anchor.position = updated.pose.position;
                current.anchor.rotation = updated.pose.rotation;
            }
        }

        /// <summary>
        /// Transoforms a ProjectedOutput into a new SessionOutput, instantiating all the required GameObjects (including the new AR Object).
        /// </summary>
        /// <param name="output">ProjectedOutput to add.</param>
        protected void AddOutput(Data.ProjectedOutput output)
        {
            GameObject newAnchorGameObject = new GameObject("DeepReality_Anchor");
            Transform newAnchor = newAnchorGameObject.GetComponent<Transform>();
            newAnchor.rotation = Quaternion.identity;
            newAnchor.localScale = Vector3.one;

            GameObject newARObject = GameObject.Instantiate(outputPrefab, Vector3.zero, Quaternion.identity, newAnchor);
            newARObject.GetComponent<Transform>().localScale = Vector3.one;

            Data.SessionOutput newSessionOutput = new Data.SessionOutput
            {
                anchor = newAnchor,
                arObject = newARObject
            };

            UpdateOutput(newSessionOutput, output, false);

            CurrentOutputs.Add(newSessionOutput);
            OnOutputAdded?.Invoke(newSessionOutput);
        }

        /// <summary>
        /// Remove an output. Also Destroys the associated AR Object.
        /// </summary>
        /// <param name="output">SessionOutput to delete</param>
        protected void RemoveOutput(Data.SessionOutput output)
        {
            GameObject.Destroy(output.anchor.gameObject);
            CurrentOutputs.Remove(output);
            OnOutputRemoved?.Invoke(output);
        }

        /// <summary>
        /// AnimationCurve used to smoothly move and rotate outputs when they are updated.
        /// </summary>
        private AnimationCurve smoothTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        /// <summary>
        /// Smoothly moves and rotates the anchor of an output to match its output's world space pose.
        /// </summary>
        /// <param name="sessionOutput">SessionOutput to transform</param>
        async void SmoothUpdatePose(Data.SessionOutput sessionOutput)
        {
            Vector3 startPosition = sessionOutput.anchor.position;
            Quaternion startRotation = sessionOutput.anchor.rotation;

            Vector3 targetPosition = sessionOutput.output.pose.position;
            Quaternion targetRotation = sessionOutput.output.pose.rotation;

            Action<float> updateAction = t =>
            {
                sessionOutput.anchor.position = Vector3.Lerp(startPosition, targetPosition, t);
                sessionOutput.anchor.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
            };

            await updateAction.AnimateAsync(sessionManager.modelExecutionInterval, smoothTransitionCurve);
        }

    }
    
}
