using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class UIFollow3DObject : MonoBehaviour
{
    public Transform mainCam;
    public Transform target;
    public Transform Canvas;
    public Vector3 offset;
    private TextMeshProUGUI textMeshProUGUI; // Pour les TextMeshPro en UI
    private TextMeshPro textMeshPro; // Pour les TextMeshPro 3D


    private void Start()
    {
        textMeshProUGUI = GetComponent<TextMeshProUGUI>();
        textMeshPro = GetComponent<TextMeshPro>();

        //transform.SetParent(worldSpaceCanvas);
    }
    void Update()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position); // look at camera
        transform.position = target.position + offset;

    }
}