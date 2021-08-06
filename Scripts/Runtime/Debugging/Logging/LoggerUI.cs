using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeepReality.Debugging.ScreenQuad;

namespace DeepReality.Debugging.Logging{

    /// <summary>
    /// Shows diagnostic information coming from DeepReality logs in a UI Text.
    /// </summary>
    public class LoggerUI : MonoBehaviour,ILogger
    {
        public Text debugText;

        public void Log(LogData data)
        {
            string debugString = "";
            debugString += $"Model processing time -> {data.modelProcessingTime} ms\n";
            debugString += $"AR projection time -> {data.arProjectionTime} ms\n";

            for (int i = 0; i < data.modelOutputs.Count; i++)
            {
                debugString += $"\n{data.modelOutputs[i].ToString()}";
            }

            debugText.text = debugString;
        }

        
    }
    
}
