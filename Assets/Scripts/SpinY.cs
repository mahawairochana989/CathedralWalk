using UnityEngine;

public class SpinY : MonoBehaviour
{
    public float degreesPerSecond = 25f;
    void Update()
    {
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
    }
}
