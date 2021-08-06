using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace DeepReality.Utils{

    /// <summary>
    /// Various image processing operations to convert images to the correct formats
    /// </summary>
    public static class ImageOperations
    {
        /// <summary>
        /// Class that contains the image's byte array and information about the various relevant aspect ratios.
        /// </summary>
        public class ByteArrayTransformationResult
        {
            public byte[] byteArray;
            public float originalAspect;
            public float newAspect;
        }

        /// <summary>
        /// Class used to store information used to transform an XrCpuImage
        /// </summary>
        class CpuImageConversionData
        {
            public byte[] byteArray;
            public int imageWidth;
            public int imageHeight;
            public Rotation rotation;
        }

        public static async Task<ByteArrayTransformationResult> ToTransformedByteArray(this XRCpuImage image, int width, int height, Matrix4x4 displayMatrix)
        {
            var data = await image.ToByteArrayAsync(displayMatrix, width, height);
            image.Dispose();

            bool flipX = false;
            bool flipY = false;

            var resArray=  await TransformRGB24ByteArrayThreadedAsync(data.byteArray, data.imageWidth, data.imageHeight, 3, width, height, data.rotation, flipX, flipY, true);
            return new ByteArrayTransformationResult
            {
                byteArray = resArray,
                originalAspect = (data.rotation == Rotation.Rotation0 || data.rotation == Rotation.Rotation180) ? (float)image.width / (float)image.height : (float)image.height / (float)image.width,
                newAspect = (float)width / (float)height
            };
        }

        static async Task<CpuImageConversionData> ToByteArrayAsync(this XRCpuImage image, Matrix4x4 displayMatrix, int? widthRequest = null, int? heightRequest = null)
        {
            CpuImageConversionData result = new CpuImageConversionData();
            result.rotation = RotationFromDisplayMatrix(displayMatrix);

            RectInt cropRect = new RectInt(0, 0, image.width, image.height);
            Vector2Int outputDimensions = new Vector2Int(cropRect.width, cropRect.height);

            if (widthRequest.HasValue && heightRequest.HasValue)
            {
                if (result.rotation == Rotation.Rotation90 || result.rotation == Rotation.Rotation270)
                {
                    int temp = widthRequest.Value;
                    widthRequest = heightRequest;
                    heightRequest = temp;
                }

                var imageAspect = (float)image.width / (float)image.height;
                var targetAspect = (float)widthRequest / (float)heightRequest;
                if (targetAspect > imageAspect)
                {
                    cropRect.height = Mathf.FloorToInt(cropRect.width / targetAspect);
                }
                else
                {
                    cropRect.width = Mathf.FloorToInt(cropRect.height * targetAspect);
                }
                cropRect.x += (image.width - cropRect.width) / 2;
                cropRect.y += (image.height - cropRect.height) / 2;

                if (cropRect.width > widthRequest && cropRect.height > heightRequest)
                {
                    outputDimensions.x = widthRequest.Value;
                    outputDimensions.y = heightRequest.Value;
                }
                else
                {
                    outputDimensions.x = cropRect.width;
                    outputDimensions.y = cropRect.height;
                }
            }

            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = cropRect,
                outputDimensions = outputDimensions,
                outputFormat = TextureFormat.RGB24,

#if UNITY_ANDROID
                transformation = XRCpuImage.Transformation.MirrorY
#else
                transformation = XRCpuImage.Transformation.None
#endif
            };

            // See how many bytes you need to store the final image.
            int size = image.GetConvertedDataSize(conversionParams);

            TaskCompletionSource<byte[]> convesrionTCS = new TaskCompletionSource<byte[]>();

            image.ConvertAsync(conversionParams, (s, p, a) =>
            {
                switch (s)
                {
                    case XRCpuImage.AsyncConversionStatus.Ready:
                        convesrionTCS.SetResult(a.ToArray());
                        break;
                    default:
                        convesrionTCS.SetResult(null);
                        break;
                }
            });



            result.imageWidth = outputDimensions.x;
            result.imageHeight = outputDimensions.y;
            result.byteArray = await convesrionTCS.Task;

            return result;
        }

        unsafe static byte[] ToByteArray(this XRCpuImage image, out int outputWidth, out int outputHeight, out Rotation outputRotation, Matrix4x4 displayMatrix, int? widthRequest = null, int? heightRequest=null)
        {

            outputRotation = RotationFromDisplayMatrix(displayMatrix);

            RectInt cropRect = new RectInt(0, 0, image.width, image.height);
            Vector2Int outputDimensions = new Vector2Int(cropRect.width, cropRect.height);

            if(widthRequest.HasValue && heightRequest.HasValue)
            {
                if(outputRotation == Rotation.Rotation90 || outputRotation == Rotation.Rotation270)
                {
                    int temp = widthRequest.Value;
                    widthRequest = heightRequest;
                    heightRequest = temp;
                }

                var imageAspect = (float)image.width / (float)image.height;
                var targetAspect = (float)widthRequest / (float)heightRequest;
                if (targetAspect > imageAspect)
                {
                    cropRect.height = Mathf.FloorToInt(cropRect.width / targetAspect);
                }
                else
                {
                    cropRect.width = Mathf.FloorToInt(cropRect.height * targetAspect);
                }
                cropRect.x += (image.width - cropRect.width) / 2;
                cropRect.y += (image.height - cropRect.height) / 2;

                if(cropRect.width > widthRequest && cropRect.height > heightRequest)
                {
                    outputDimensions.x = widthRequest.Value;
                    outputDimensions.y = heightRequest.Value;
                }
                else
                {
                    outputDimensions.x = cropRect.width;
                    outputDimensions.y = cropRect.height;
                }
            }

            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = cropRect,
                outputDimensions = outputDimensions,
                outputFormat = TextureFormat.RGB24,

#if UNITY_ANDROID
                transformation = XRCpuImage.Transformation.MirrorY
#else
                transformation = XRCpuImage.Transformation.None
#endif
            };

            // See how many bytes you need to store the final image.
            int size = image.GetConvertedDataSize(conversionParams);

            // Allocate a buffer to store the image.
            using (var buffer = new NativeArray<byte>(size, Allocator.Temp))
            {

                // Extract the image data
                image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);

                outputWidth = outputDimensions.x;
                outputHeight = outputDimensions.y;
                return buffer.ToArray();
            }
        }

        [System.Serializable]
        public enum Rotation
        {
            Rotation0,
            Rotation90,
            Rotation180,
            Rotation270,
        }

        static Rotation RotationFromDisplayMatrix(Matrix4x4 matrix)
        {
#if UNITY_ANDROID

            // 1 0 0 Landscape Left (upside down)
            // 0 1 0
            // 0 0 0
            if (Mathf.RoundToInt(matrix[0, 0]) == 1 && Mathf.RoundToInt(matrix[1, 1]) == 1)
            {
                return Rotation.Rotation180;
            }

            //-1 0 1 Landscape Right
            // 0-1 1
            // 0 0 0
            else if (Mathf.RoundToInt(matrix[0, 0]) == -1 && Mathf.RoundToInt(matrix[1, 1]) == -1)
            {
                return Rotation.Rotation0;
            }

            // 0 1 0 Portrait
            //-1 0 1
            // 0 0 0
            else if (Mathf.RoundToInt(matrix[0, 1]) == 1 && Mathf.RoundToInt(matrix[1, 0]) == -1)
            {
                return Rotation.Rotation270;
            }

            // 0-1 1 Portrait (upside down)
            // 1 0 0
            // 0 0 0
            else if (Mathf.RoundToInt(matrix[0, 1]) == -1 && Mathf.RoundToInt(matrix[1, 0]) == 1)
            {
                return Rotation.Rotation90;
            }

#elif UNITY_IOS

            // 0-.6 0 Portrait
            //-1  1 0 The source image is upside down as well, so this is identity
            // 1 .8 1
            if (Mathf.RoundToInt(matrix[0,0]) == 0)
            {
                return Rotation.Rotation90;
            }

            //-1  0 0 Landscape Right
            // 0 .6 0
            // 1 .2 1
            else if (Mathf.RoundToInt(matrix[0,0]) == -1)
            {
                return Rotation.Rotation0;
            }

            // 1  0 0 Landscape Left
            // 0-.6 0
            // 0 .8 1
            else if (Mathf.RoundToInt(matrix[0,0]) == 1)
            {
                return Rotation.Rotation180;
            }

            // iOS has no upside down?
#endif
            return Rotation.Rotation0;

        }

        public static async Task<byte[]> TransformRGB24ByteArrayAsync(byte[] input, int inputWidth, int inputHeight, int bytesPerPixel, int outputWidth, int outputHeight, Rotation rotation, bool flipX, bool flipY, bool crop)
        {
            return await Task.Run(() => TransformRGB24ByteArray(input, inputWidth, inputHeight, bytesPerPixel, outputWidth, outputHeight, rotation, flipX, flipY, crop));
        }

        public static byte[] TransformRGB24ByteArray(byte[] input, int inputWidth, int inputHeight, int bytesPerPixel, int outputWidth, int outputHeight, Rotation rotation, bool flipX, bool flipY, bool crop)
        {
            byte[] output = new byte[outputWidth * outputHeight * bytesPerPixel];
            

            int x, y, c;

            RotateRGB24ByteArray(ref input, ref inputWidth, ref inputHeight, bytesPerPixel, rotation);



            if (crop)
            {
                CropRGB24ByteArray(ref input, ref inputWidth, ref inputHeight, bytesPerPixel, (float)outputWidth / (float)outputHeight);
            }

            float ratioX = 1.0f / ((float)outputWidth / (inputWidth - 1));
            float ratioY = 1.0f / ((float)outputHeight / (inputHeight - 1));

            for (y = 0; y < outputHeight; y++)
            {
                int yFloor = (int)Mathf.Floor(y * ratioY);
                var y1 = yFloor * inputWidth * bytesPerPixel;
                var y2 = (yFloor + 1) * inputWidth * bytesPerPixel;
                var yw = y * outputWidth * bytesPerPixel;

                for (x = 0; x < outputWidth; x++)
                {
                    int xFloor = (int)Mathf.Floor(x * ratioX);
                    var xLerp = x * ratioX - xFloor;

                    for(c = 0;c<bytesPerPixel;c++)
                    {
                        output[yw + x * bytesPerPixel + c] = ByteLerpUnclamped(
                            ByteLerpUnclamped(input[y1 + xFloor * bytesPerPixel + c], input[y1 + xFloor * bytesPerPixel + bytesPerPixel + c], xLerp),
                            ByteLerpUnclamped(input[y2 + xFloor * bytesPerPixel + c], input[y2 + xFloor * bytesPerPixel + bytesPerPixel + c], xLerp),
                            y * ratioY - yFloor);
                    }
                }
            }

            return output;
        }

        public async static Task<byte[]> TransformRGB24ByteArrayThreadedAsync(byte[] input, int inputWidth, int inputHeight, int bytesPerPixel, int outputWidth, int outputHeight, Rotation rotation, bool flipX, bool flipY, bool crop)
        {
            byte[] output = new byte[outputWidth * outputHeight * bytesPerPixel];


            //int x, y, c;

            RotateRGB24ByteArray(ref input, ref inputWidth, ref inputHeight, bytesPerPixel, rotation);



            if (crop)
            {
                await Task.Run(()=> CropRGB24ByteArray(ref input, ref inputWidth, ref inputHeight, bytesPerPixel, (float)outputWidth / (float)outputHeight));
            }

            if (outputWidth == inputWidth && outputHeight == inputHeight) return input;

            float ratioX = 1.0f / ((float)outputWidth / (inputWidth - 1));
            float ratioY = 1.0f / ((float)outputHeight / (inputHeight - 1));

            int threads = Mathf.Min(SystemInfo.processorCount, outputHeight); 
            int parcelSize = outputHeight / threads;

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < threads; i++)
            {
                int startY = parcelSize * i;
                int endY = parcelSize * (i + 1);
                if (i == threads - 1) endY = outputHeight;

                tasks.Add(Task.Run(() => ThreadedByteArrayScaling(input, output, inputWidth, inputHeight, bytesPerPixel, outputWidth, outputHeight, startY, endY, ratioX, ratioY)));
            }

            await Task.WhenAll(tasks);

            return output;
        }

        static void ThreadedByteArrayScaling(byte[] input, byte[] output, int inputWidth, int inputHeight, int bytesPerPixel, int outputWidth, int outputHeight, int startY, int endY, float ratioX, float ratioY)
        {
            for (int y = startY; y < endY; y++)
            {
                int yFloor = (int)Mathf.Floor(y * ratioY);
                var y1 = yFloor * inputWidth * bytesPerPixel;
                var y2 = (yFloor + 1) * inputWidth * bytesPerPixel;
                var yw = y * outputWidth * bytesPerPixel;

                for (int x = 0; x < outputWidth; x++)
                {
                    int xFloor = (int)Mathf.Floor(x * ratioX);
                    var xLerp = x * ratioX - xFloor;

                    for (int c = 0; c < bytesPerPixel; c++)
                    {
                        output[yw + x * bytesPerPixel + c] = ByteLerpUnclamped(
                            ByteLerpUnclamped(input[y1 + xFloor * bytesPerPixel + c], input[y1 + xFloor * bytesPerPixel + bytesPerPixel + c], xLerp),
                            ByteLerpUnclamped(input[y2 + xFloor * bytesPerPixel + c], input[y2 + xFloor * bytesPerPixel + bytesPerPixel + c], xLerp),
                            y * ratioY - yFloor);
                    }
                }
            }
        }

        static void RotateRGB24ByteArray(ref byte[] input, ref int inputWidth, ref int inputHeight, int bytesPerPixel, Rotation rotation)
        {
            if (rotation == Rotation.Rotation0) return;


            byte[] rotated = new byte[inputWidth * inputHeight * bytesPerPixel];
            int rotX = 0;
            int rotY = 0;
            int rotWidth = inputWidth;
            int rotHeight = inputHeight;
            for (int y = 0; y < inputHeight; y++)
            {
                for (int x = 0; x < inputWidth; x++)
                {
                    switch (rotation)
                    {
                        case Rotation.Rotation270:
                            rotY = x;
                            rotX = inputHeight - (y + 1);
                            rotWidth = inputHeight;
                            rotHeight = inputWidth;
                            break;
                        case Rotation.Rotation180:
                            rotY = inputHeight - (y + 1);
                            rotX = inputWidth - (x + 1);
                            break;
                        case Rotation.Rotation90:
                            rotY = inputWidth - (x + 1);
                            rotX = y;
                            rotWidth = inputHeight;
                            rotHeight = inputWidth;
                            break;
                    }

                    for (int c = 0; c < bytesPerPixel; c++)
                    {
                        rotated[(rotX + rotY * rotWidth) * bytesPerPixel + c] = input[(x + y * inputWidth) * bytesPerPixel + c];
                    }
                }
            }
            inputWidth = rotWidth;
            inputHeight = rotHeight;
            input = rotated;
        }

        static void CropRGB24ByteArray(ref byte[] input, ref int inputWidth, ref int inputHeight, int bytesPerPixel, float targetAspect)
        {
            var imageAspect = (float)inputWidth / (float)inputHeight;
            //var targetAspect = (float)outputWidth / (float)outputHeight;

            if (Mathf.Abs(imageAspect - targetAspect) < Mathf.Epsilon) return;

            int cropWidth;
            int cropHeight;
            int cropX;
            int cropY;

            if (targetAspect > imageAspect)
            {
                cropWidth = inputWidth;
                cropHeight = Mathf.FloorToInt(cropWidth / targetAspect);
            }
            else
            {
                cropHeight = inputHeight;
                cropWidth = Mathf.FloorToInt(cropHeight * targetAspect);
            }

            cropX = (inputWidth - cropWidth) / 2;
            cropY = (inputHeight - cropHeight) / 2;

            byte[] result = new byte[cropWidth * cropHeight * bytesPerPixel];
            for (int y = 0; y < cropHeight; y++)
            {
                for (int x = 0; x < cropWidth; x++)
                {
                    for (int c = 0; c < bytesPerPixel; c++)
                    {
                        result[(x + y * cropWidth) * bytesPerPixel + c] = input[(x + cropX + (y + cropY) * inputWidth) * bytesPerPixel + c];
                    }
                }
            }

            inputWidth = cropWidth;
            inputHeight = cropHeight;

            input = result;
        }

        static byte ByteLerpUnclamped(byte b1, byte b2, float value)
        {
            return  (byte)Mathf.Clamp((int)(b1 + (b2 - b1)*value),byte.MinValue,byte.MaxValue);
        }

    }
    
}
