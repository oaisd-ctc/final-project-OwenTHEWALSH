using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightFollower : MonoBehaviour
{
    [Tooltip("The transform to follow. If null, will follow the parent.")]
    [SerializeField]
    private Transform targetToFollow;

    [Tooltip("Offset from the target position.")]
    [SerializeField]
    private Vector3 positionOffset = Vector3.zero;

    [Tooltip("Should the light also match the target's rotation?")]
    [SerializeField]
    private bool followRotation = true;

    private void Start()
    {
        if (targetToFollow == null)
        {
            targetToFollow = transform.parent;
        }

        if (targetToFollow == null)
        {
            Debug.LogWarning("LightFollower: No target assigned and no parent found. Light will not follow anything.");
        }
    }

    private void LateUpdate()
    {
        if (targetToFollow == null)
            return;

        transform.position = targetToFollow.position + positionOffset;

        if (followRotation)
        {
            transform.rotation = targetToFollow.rotation;
        }
    }
}