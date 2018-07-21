using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class DebugMotionDrawer : MonoBehaviour
{
    private Vector3 _lastPosition;
    public Animator Animator;
    void Start()
    {
        _lastPosition = transform.position;
        Animator = GetComponent<Animator>();
    }

    void Update()
    {
        Debug.DrawLine(_lastPosition, transform.position, Color.blue, 2f);
        _lastPosition = transform.position;
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.1f);

        if (Animator != null)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(Animator.rootPosition, 0.1f);
        }
    }

}
