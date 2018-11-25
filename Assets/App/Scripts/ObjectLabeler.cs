using System.Collections.Generic;
using CustomVison;
using HoloToolkit.Unity.SpatialMapping;
using HoloToolkit.UX.ToolTips;
using HoloToolkitExtensions.Messaging;
using UnityEngine;

public class ObjectLabeler : MonoBehaviour
{
    private List<GameObject> _createdObjects = new List<GameObject>();

    [SerializeField]
    private GameObject _labelObject;

    [SerializeField]
    private GameObject _labelContainer;

    [SerializeField]
    private string _labelText = "Toy aircraft";

    [SerializeField]
    private GameObject _debugObject;

    private void Start()
    {
        Messenger.Instance.AddListener<ObjectRecognitionResultMessage>(
            p => LabelObjects(p.Predictions, p.CameraResolution, p.CameraTransform));
    }

    public virtual void LabelObjects(IList<Prediction> predictions, 
        Resolution cameraResolution, Transform cameraTransform)
    {
        ClearLabels();
        var heightFactor = cameraResolution.height / cameraResolution.width;
        var topCorner = cameraTransform.position + cameraTransform.forward -
                        cameraTransform.right / 2f +
                        cameraTransform.up * heightFactor / 2f;
        foreach (var prediction in predictions)
        {
            var center = prediction.GetCenter();
            var recognizedPos = topCorner + cameraTransform.right * center.x -
                                cameraTransform.up * center.y * heightFactor;

#if UNITY_EDITOR
             _createdObjects.Add(CreateLabel(_labelText, recognizedPos));
#endif
            var labelPos = DoRaycastOnSpatialMap(cameraTransform, recognizedPos);
            if (labelPos != null)
            {
                _createdObjects.Add(CreateLabel(_labelText, labelPos.Value));
            }
        }

        if (_debugObject != null)
        {
             _debugObject.SetActive(false);
        }

        Destroy(cameraTransform.gameObject);
    }

    private Vector3? DoRaycastOnSpatialMap(Transform cameraTransform, Vector3 recognitionCenterPos)
    {
        RaycastHit hitInfo;

        if (SpatialMappingManager.Instance != null && 
            Physics.Raycast(cameraTransform.position, (recognitionCenterPos - cameraTransform.position), 
                out hitInfo, 10, SpatialMappingManager.Instance.LayerMask))
        {
            return hitInfo.point;
        }
        return null;
    }

    private void ClearLabels()
    {
        foreach (var label in _createdObjects)
        {
            Destroy(label);
        }
        _createdObjects.Clear();
    }

    private GameObject CreateLabel(string text, Vector3 location)
    {
        var labelObject = Instantiate(_labelObject);
        var toolTip = labelObject.GetComponent<ToolTip>();
        toolTip.ShowOutline = false;
        toolTip.ShowBackground = true;
        toolTip.ToolTipText = text;
        toolTip.transform.position = location + Vector3.up * 0.2f;
        toolTip.transform.parent = _labelContainer.transform;
        toolTip.AttachPointPosition = location;
        toolTip.ContentParentTransform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        var connector = toolTip.GetComponent<ToolTipConnector>();
        connector.PivotDirectionOrient = ConnectorOrientType.OrientToCamera;
        connector.Target = labelObject;
        return labelObject;
    }
}

