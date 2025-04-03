using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIFollowCamera : MonoBehaviour
{
    public Transform mainCam;
    public Transform canvas;

    void Update()
    {
        // Définir la position du canvas pour qu'il suive la caméra
        canvas.position = mainCam.position + new Vector3(0, 0, -5f);
        // Définir la rotation du canvas pour qu'il regarde la caméra
        canvas.rotation = mainCam.rotation;
    }
}
