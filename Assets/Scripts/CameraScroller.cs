using UnityEngine;

public class CameraScroller : MonoBehaviour
{
	public float scrollSpeed = 1.5f; // Speed at which the camera scrolls
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.forward * scrollSpeed * Time.deltaTime);
    }
}
