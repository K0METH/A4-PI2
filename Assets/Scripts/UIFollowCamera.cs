using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIFollowCamera : MonoBehaviour
{
    public Transform mainCam;
    public Transform canvas;

    void Update()
    {
        // D�finir la position du canvas pour qu'il suive la cam�ra
        canvas.position = mainCam.position + new Vector3(0, 0, -5f);
        // D�finir la rotation du canvas pour qu'il regarde la cam�ra
        canvas.rotation = mainCam.rotation;
    }
}
