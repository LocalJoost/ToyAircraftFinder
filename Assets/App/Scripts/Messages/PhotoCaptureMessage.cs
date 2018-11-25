
using System.Collections.Generic;
using UnityEngine;

public class PhotoCaptureMessage : ObjectRecognitionMessageBase
{
    public IList<byte> Image { get; protected set; }

    public PhotoCaptureMessage(IList<byte> image, 
        Resolution cameraResolution, Transform cameraTransform) : 
        base( cameraResolution, cameraTransform)
    {
        Image = image;
    }
}
