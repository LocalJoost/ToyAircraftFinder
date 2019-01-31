
using System.Collections.Generic;
using CustomVision;
using UnityEngine;

public class ObjectRecognitionResultMessage : ObjectRecognitionMessageBase
{
    public IList<PredictionModel> Predictions { get; protected set; }


    public ObjectRecognitionResultMessage(IList<PredictionModel> predictions, 
        Resolution cameraResolution, Transform cameraTransform) : 
        base( cameraResolution, cameraTransform)
    {
        Predictions = predictions;
    }
}
