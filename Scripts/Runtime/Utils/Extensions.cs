using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DeepReality.Utils{
    
    public static class Extensions
    {
        /// <summary>
        /// Helper method to perform simple animations.
        /// </summary>
        static async Task AnimateAsync(float time, AnimationCurve curve, Action<float> updateAction)
        {
            float t = 0;
            updateAction(0f);

            while (t < 1f)
            {
                await AsyncUtils.WaitForUpdate();
                t += Time.deltaTime / time;

                updateAction(curve?.Evaluate(t) ?? t);
            }
        }

        /// <summary>
        /// Helper method to perform simple animations.
        /// </summary>
        public static async Task AnimateAsync(this Action<float> updateAction, float time, AnimationCurve curve)
        {
            await AnimateAsync(time, curve, updateAction);
        }

        /// <summary>
        /// Calculate the average of a set of Vector3.
        /// </summary>
        /// <param name="vectors">Vector3s to average.</param>
        /// <returns></returns>
        public static Vector3 Average(this IEnumerable<Vector3> vectors)
        {
            Vector3 result = Vector3.zero;
            int amount = 0;
            foreach (var v in vectors)
            {
                result += v;
                amount++;
            }

            return result / (float)amount;
        }

        /// <summary>
        /// Calculate the average of a set of Quaternions.
        /// </summary>
        /// <param name="quaternions">Quaternions to average.</param>
        /// <returns></returns>
        public static Quaternion Average(this IEnumerable<Quaternion> quaternions)
        {
            Quaternion average = new Quaternion(0, 0, 0, 0);

            int amount = 0;

            foreach (var quaternion in quaternions)
            {
                amount++;

                average = Quaternion.Slerp(average, quaternion, 1f / (float)amount);
            }

            return average;
        }
    }
    
}
