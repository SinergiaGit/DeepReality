using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeepReality.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace DeepReality.Debugging.FrameFeeders{

    /// <summary>
    /// Feeds the DeepReality session with images taken from a sequence of textures.
    /// When the sequence ends, it loops.
    /// </summary>
    public class ImageSequenceFrameFeeder : MonoBehaviour, IDebugFrameFeeder
    {

        public List<Texture2D> sequence;
        public GameObject debugBackgoundRoot;
        public int framesPerPhoto=5;

        RawImage backgroundImage;
        AspectRatioFitter aspectFitter;

        int index = 0;
        int frameCount = 0;

        public bool letterbox = false;

        private void Awake()
        {

            backgroundImage = debugBackgoundRoot.GetComponentInChildren<RawImage>();
            aspectFitter = debugBackgoundRoot.GetComponentInChildren<AspectRatioFitter>();

            if(letterbox)
            {
                for(int i = 0; i < sequence.Count; i++)
                {
                    sequence[i] = LetterboxTexture(sequence[i]);
                }
            }
        }

        void OnDestroy()
        {
            Destroy(debugBackgoundRoot);
        }

        public async Task<ImageOperations.ByteArrayTransformationResult> GetFrameAsync(int requiredWidth, int requiredHeight)
        {
            await AsyncUtils.WaitForMainThreadAsync();

            index = index % sequence.Count;

            Texture2D newTexture = sequence[index];

            

            byte[] imageArray = newTexture.GetRawTextureData();

            imageArray = await ImageOperations.TransformRGB24ByteArrayThreadedAsync(imageArray, newTexture.width, newTexture.height, 3, requiredWidth, requiredHeight, ImageOperations.Rotation.Rotation0, false, false, true);

            var result = new ImageOperations.ByteArrayTransformationResult
            {
                byteArray = imageArray,
                originalAspect = (float)newTexture.width / (float)newTexture.height,
                newAspect = (float)requiredWidth / (float)requiredHeight
            };

            backgroundImage.texture = newTexture;
            aspectFitter.aspectRatio = result.originalAspect;

            frameCount++;
            if (frameCount == framesPerPhoto)
            {
                frameCount = 0;
                index++;
            }
            return result;
        }

        Texture2D LetterboxTexture(Texture2D tex)
        {
            int max = Mathf.Max(tex.width, tex.height);
            Texture2D output = new Texture2D(max, max, TextureFormat.RGB24, false);

            int x = (max - tex.width) / 2;
            int y = (max - tex.height) / 2;

            output.SetPixels32(x, y, tex.width, tex.height, tex.GetPixels32());
            output.Apply();

            Destroy(tex);
            return output;

        }
        
    }
    
}
