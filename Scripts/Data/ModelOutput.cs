using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeepReality.Data{

    /// <summary>
    /// Output of the ML Model.
    /// </summary>
    [System.Serializable]
    public class ModelOutput
    {
        /// <summary>
        /// Normalized screen space rect of the recognized area.
        /// </summary>
        public Rect screenRect;
        /// <summary>
        /// Description string of what was recognized.
        /// </summary>
        public string description;
        /// <summary>
        /// Additional data relevant to the recognition.
        /// </summary>
        public object data;
        /// <summary>
        /// Reported confidence of the recognition.
        /// </summary>
        public float confidence;

        public override string ToString()
        {
            return $"{description} [{confidence}]: {screenRect.position}";
        }

        /// <summary>
        /// Converts the "screenRect" area from the aspect ratio used by the ML Model to the aspect ratio of the image visible by the user.
        /// </summary>
        /// <param name="originalAspect">Original aspect ratio of the image.</param>
        /// <param name="processingAspect">Aspect ratio used by the ML Model.</param>
        /// <param name="screenAspect">Aspect ratio of the screen.</param>
        public void AdjustRect(float originalAspect, float processingAspect, float screenAspect)
        {
            AdjustRect(processingAspect, originalAspect,true);
            AdjustRect(originalAspect, screenAspect,false);
        }

        void AdjustRect(float aspect1, float aspect2, bool isAspect1Contained)
        {
            float mul =  aspect1 / aspect2;


            float mulX = 1f;
            float mulY = 1f;

            if (mul < 1)
            {
                if (isAspect1Contained)
                {
                    mulX = 1f / mul;
                }
                else
                {
                    mulY = mul;
                }
            }
            else
            {
                if (isAspect1Contained)
                    mulY = mul;
                else
                    mulX = 1f / mul;
            }



            screenRect.width /= mulX;
            screenRect.height /= mulY;
            screenRect.x += (mulX-1) / 2f;
            screenRect.x /= mulX;
            screenRect.y += (mulY-1) / 2f;
            screenRect.y /= mulY;
        }

        /// <summary>
        /// Get the "screenRect" in screen space (not normalized).
        /// </summary>
        /// <param name="screenWidth">Width of the screen.</param>
        /// <param name="screenHeight">Height of the screem.</param>
        /// <returns>"screenRect" in screen space (not normalized)</returns>
        public Rect GetDenormalizedScreenRect(float screenWidth, float screenHeight)
        {
            Rect denormalized = screenRect;

            denormalized.x *= screenWidth;
            denormalized.width *= screenWidth;

            denormalized.y *= screenHeight;
            denormalized.height *= screenHeight;

            return denormalized;
        }
    }
    
}
