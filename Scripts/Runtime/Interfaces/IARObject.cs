using System.Collections;
using System.Collections.Generic;
using DeepReality.Data;
using UnityEngine;

namespace DeepReality.Interfaces{

    /// <summary>
    /// Interface that MonoBehaviours need to implement if they want to receive
    /// data regarding the ProjectedOutput they represent.
    /// MonoBehaviours implementing this interface should be used on the GameObjects
    /// instantiated by DeepReality upon the recognition of an object.
    /// </summary>
    public interface IARObject
    {
        /// <summary>
        /// Called to enable the component to respond to changes of the ProjectedOutput they represent.
        /// Also called when the GameObject is first instantiated.
        /// </summary>
        /// <param name="output">The ProjectedOutput that this object is representing in th scene.</param>
        void UpdateData(ProjectedOutput output);
    }
    
}
