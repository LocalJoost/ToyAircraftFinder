using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CustomVision;
using HoloToolkitExtensions.Messaging;
using UnityEngine;
#if UNITY_WSA && !UNITY_EDITOR
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media;
#endif

public class ObjectRecognizer : MonoBehaviour
{
#if UNITY_WSA && !UNITY_EDITOR
    private ObjectDetection _objectDetection;
#endif

    private bool _isInitialized;

    private void Start()
    {
        Messenger.Instance.AddListener<PhotoCaptureMessage>(p=> RecognizeObjects(p.Image, p.CameraResolution, p.CameraTransform));
#if UNITY_WSA && !UNITY_EDITOR

        _objectDetection = new ObjectDetection(new[]{"aircraft"}, 20, 0.5f,0.3f );
        Debug.Log("Initializing...");
        _objectDetection.Init("ms-appx:///Data/StreamingAssets/model.onnx").ContinueWith(p =>
        {
            Debug.Log("Intializing ready");
            _isInitialized = true;
        });
#endif
    }
    
    public virtual void RecognizeObjects(IList<byte> image, 
                                         Resolution cameraResolution, 
                                         Transform cameraTransform)
    {
        if (_isInitialized)
        {
#if UNITY_WSA && !UNITY_EDITOR
            RecognizeObjectsAsync(image, cameraResolution, cameraTransform);
#endif

        }
    }

#if UNITY_WSA && !UNITY_EDITOR

    private async Task RecognizeObjectsAsync(IList<byte> image, Resolution cameraResolution, Transform cameraTransform)
    {
        //https://stackoverflow.com/questions/35070622/photo-capture-stream-to-softwarebitmap
        //https://blogs.msdn.microsoft.com/appconsult/2018/05/23/add-a-bit-of-machine-learning-to-your-windows-application-thanks-to-winml/
        using (var stream = new MemoryStream(image.ToArray()))
        {
            var decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
            var sfbmp = await decoder.GetSoftwareBitmapAsync();
            sfbmp = SoftwareBitmap.Convert(sfbmp, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            var picture = VideoFrame.CreateWithSoftwareBitmap(sfbmp);
            var prediction = await _objectDetection.PredictImageAsync(picture);
            ProcessPredictions(prediction, cameraResolution, cameraTransform);
        }
    }
#endif

#if UNITY_WSA && !UNITY_EDITOR

    private void ProcessPredictions(IList<PredictionModel>predictions, Resolution cameraResolution, Transform cameraTransform)
    {
        var acceptablePredications = predictions.Where(p => p.Probability >= 0.7).ToList();
        Messenger.Instance.Broadcast(
         new ObjectRecognitionResultMessage(acceptablePredications, cameraResolution, cameraTransform));
    }
#endif

}

