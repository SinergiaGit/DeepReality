using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DeepReality.Debugging.ScreenQuad
{
    public class ScreenQuad : MonoBehaviour
    {
        public Text text;

        [SerializeField]
        private ScreenQuadData data;


        RectTransform myTransform;


        public void SetQuad(ScreenQuadData data)
        {
            this.data = data;
            text.text = data.name;

            UpdateTransform();
        }

        void UpdateTransform()
        {
            myTransform = GetComponent<RectTransform>();

            myTransform.localScale = Vector3.one;

            myTransform.anchorMin = data.rect.min;
            myTransform.anchorMax = data.rect.max;

            myTransform.anchoredPosition = Vector2.zero;
            myTransform.sizeDelta = Vector2.zero;

        }

        
    }
    
}
