using LearnXR.Core.Utilities;
using UnityEngine;

[RequireComponent(typeof(OVRMicrogestureEventSource))]
public class MicrogestureListener : MonoBehaviour
{
    private OVRMicrogestureEventSource ovrMicrogestureEventSource;
    
    void Start()
    {
        ovrMicrogestureEventSource = GetComponent<OVRMicrogestureEventSource>();
        ovrMicrogestureEventSource.GestureRecognizedEvent.AddListener(g =>
        {
            LogMicrogestureEvent($"{g}");
        });
    }

    public void LogMicrogestureEvent(string microgestureName)
    {
        var message = $"Microgesture event received: {microgestureName}";
        SpatialLogger.Instance.LogInfo(message);
        Debug.Log(message);
    }
}
