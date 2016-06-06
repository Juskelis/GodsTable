using UnityEngine;
using System.Collections;

public class GliderController : MonoBehaviour
{
    [SerializeField]
    private float rollSpeed;

    [SerializeField]
    private float flipSpeed;

    void Update()
    {
        transform.Rotate(
            Input.GetAxis("Vertical")*flipSpeed,
            0, 
            -1*Input.GetAxis("Horizontal")*rollSpeed
        );
    }
}
