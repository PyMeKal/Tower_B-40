using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGParallax : MonoBehaviour
{
    public Transform parentFolder;
    [SerializeField] private float parallaxZMultiplier;
    private List<Transform> parallaxTransforms = new List<Transform>();

    private Vector3 prevCameraPos;

    private Transform cameraTransform;

    [SerializeField] private bool xOnly = true;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < parentFolder.childCount; i++)
        {
            parallaxTransforms.Add(parentFolder.GetChild(i).transform);
        }

        cameraTransform = Camera.main.transform;
        prevCameraPos = cameraTransform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 deltaPos = cameraTransform.position - prevCameraPos;
        if (xOnly) deltaPos.y = 0;

        foreach (var objTransform in parallaxTransforms)
        {
            float amount = (objTransform.position.z * parallaxZMultiplier) > 1f
                ? 1f
                : objTransform.position.z * parallaxZMultiplier;
            objTransform.position += deltaPos * amount;
        }

        prevCameraPos = cameraTransform.position;
    }
}
