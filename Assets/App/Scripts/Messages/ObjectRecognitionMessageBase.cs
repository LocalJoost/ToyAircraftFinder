using System.Collections.Generic;
using CustomVison;
using UnityEngine;

public class ObjectRecognitionMessageBase
{
    public Resolution CameraResolution { get; protected set; }
    public Transform CameraTransform { get; protected set; }

    public ObjectRecognitionMessageBase(
        Resolution cameraResolution, Transform cameraTransform)
    {
        CameraResolution = cameraResolution;
        CameraTransform = cameraTransform;
    }
}