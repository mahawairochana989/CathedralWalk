using UnityEngine;

public class SpinLogo : MonoBehaviour
{
    public float degreesPerSecond = 60f;
    void Update()
    {
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
    }
}
