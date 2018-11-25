using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomVison;
using HoloToolkitExtensions.Messaging;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class ObjectRecognizer : MonoBehaviour
{
    [SerializeField]
    private string _liveDataUrl = "<your custom vision app url here>";

    [SerializeField]
    private string _predictionKey = "<your prediction key here>";

    private void Start()
    {
        Messenger.Instance.AddListener<PhotoCaptureMessage>(p=> RecognizeObjects(p.Image, p.CameraResolution, p.CameraTransform));
    }

    public virtual void RecognizeObjects(IList<byte> image, Resolution cameraResolution, Transform cameraTransform)
    {
        StartCoroutine(RecognizeObjectsInternal(image, cameraResolution, cameraTransform));
    }

    private IEnumerator RecognizeObjectsInternal(IEnumerable<byte> image, 
        Resolution cameraResolution, Transform cameraTransform)
    {
        var request = UnityWebRequest.Post(_liveDataUrl, string.Empty);
        request.SetRequestHeader("Prediction-Key", _predictionKey);
        request.SetRequestHeader("Content-Type", "application/octet-stream");
        request.uploadHandler = new UploadHandlerRaw(image.ToArray());
        yield return request.SendWebRequest();
        var text = request.downloadHandler.text;
        var result = JsonConvert.DeserializeObject<CustomVisionResult>(text);
        if (result != null)
        {
            result.Predictions.RemoveAll(p => p.Probability < 0.7);
            Debug.Log("#Predictions = " + result.Predictions.Count);
            Messenger.Instance.Broadcast(
                new ObjectRecognitionResultMessage(result.Predictions, cameraResolution, cameraTransform));
        }
        else
        {
            Debug.Log("Predictions is null");
        }
    }
}

