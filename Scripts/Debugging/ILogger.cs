using System;

namespace DeepReality.Debugging
{
    /// <summary>
    /// Interface that must be implemented by all the MonoBehaviours that want to receive diagnostic logs from the DeepRealiti session.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Method called when a new log is generated.
        /// </summary>
        /// <param name="data">Diagnostic data</param>
        void Log(LogData data);
    }
    
}
