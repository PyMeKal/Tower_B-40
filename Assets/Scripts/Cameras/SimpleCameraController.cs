using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCameraController : MonoBehaviour
{
    public float speed;
    public float zoomSpeed;
    private Camera thisCamera;
    public bool followBestAgent;

    void Start()
    {
        thisCamera = GetComponent<Camera>();
    }
    void Update()
    {
        Vector3 moveVector = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * (speed * Time.unscaledDeltaTime);
        transform.Translate(moveVector);

        if (Input.mouseScrollDelta.y < 0f) thisCamera.orthographicSize += zoomSpeed * Time.unscaledDeltaTime;
        else if (Input.mouseScrollDelta.y > 0f) thisCamera.orthographicSize -= zoomSpeed * Time.unscaledDeltaTime;
    }
}
