
using System.Collections.Generic;
using CustomVison;
using UnityEngine;

public class ObjectRecognitionResultMessage : ObjectRecognitionMessageBase
{
    public IList<Prediction> Predictions { get; protected set; }


    public ObjectRecognitionResultMessage(IList<Prediction> predictions, 
        Resolution cameraResolution, Transform cameraTransform) : 
        base( cameraResolution, cameraTransform)
    {
        Predictions = predictions;
    }
}
