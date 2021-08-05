using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DeepReality.Debugging.ScreenQuad
{

    /// <summary>
    /// Simple class used to draw the rects of the ModelOutputs on the screen (using the UI).
    /// Receives all the necessary data through the ILogger interface.
    /// </summary>
    public class ScreenQuadManager : MonoBehaviour,ILogger
    {
        RectTransform myTransform;

        public GameObject quadPrefab;

        private void Awake()
        {
            myTransform = GetComponent<RectTransform>();
        }


        public void SetQuads(List<Data.ModelOutput> modelOutputs)
        {
            SetQuads(modelOutputs.Select(o => new ScreenQuadData
            {
                name = o.description,
                rect = o.screenRect
            }).ToList());
        }

        public void SetQuads(List<ScreenQuadData> quads)
        {
            ClearChildren();

            foreach(var q in quads)
            {
                var newObject = Instantiate(quadPrefab, Vector3.zero, Quaternion.identity, myTransform);
                newObject.GetComponent<ScreenQuad>().SetQuad(q);
            }
        }

        void ClearChildren()
        {
            foreach(var t in myTransform)
            {
                if (t is Transform tr) Destroy(tr.gameObject);
            }
        }

        public void Log(LogData data)
        {
            SetQuads(data.modelOutputs);
        }
    }

    [System.Serializable]
    public class ScreenQuadData
    {
        public Rect rect;
        public string name;
    }

}
