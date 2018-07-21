using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions.Must;
using UnityEngine.EventSystems;

public class AnimatorController : MonoBehaviour
{
    [Header("Setup")]
    public Animator Animator;
    public Rigidbody Body;
    public NavMeshAgent Agent;

    [Header("Config")]
    [Range(1f, 4f)]
    public float MovingTurnSpeed = 360;
    public float StationaryTurnSpeed = 180;
    public float RunCycleLegOffset = 0.2f;
    public float MoveSpeedMultiplier = 1f;
    public float AnimSpeedMultiplier = 1f;

    private const float KHalf = 0.5f;
    private bool _mIsGrounded;
    private float _mTurnAmount;
    private float _mForwardAmount;
    private float _mCapsuleHeight;
    private Vector3 _mCapsuleCenter;
    private Vector3 _groundPosition;
    private bool _isGrounded;
    private float _height;
    private float _linkYDiff;
    private float _yDiffRemainingAbs;
    private float _groundDistance;
    private Vector3 _mGroundNormal;
    private Vector3 _launchPosition;
    public Parameters Parameters { get; set; }

    private void Awake()
    {
        if (Animator == null)
        {
            Animator = GetComponent<Animator>();
        }
        if (Body == null)
        {
            Body = GetComponent<Rigidbody>();
        }
        if (Agent == null)
        {
            Agent = GetComponent<NavMeshAgent>();
        }
        Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        Parameters = new Parameters(Animator);
    }
    public void UpdateAnimation(Vector3 move)
    {
        if (move.magnitude > 1f)
            move.Normalize();

        move = transform.InverseTransformDirection(move);
        CheckGroundStatus();
        move = Vector3.ProjectOnPlane(move, _mGroundNormal);
        _mTurnAmount = Mathf.Atan2(move.x, move.z);
        _mForwardAmount = move.z;

        ApplyExtraTurnRotation();
        UpdateAnimatorParameters(move);
    }

    private void UpdateAnimatorParameters(Vector3 move)
    {
        Parameters.Forward = _mForwardAmount;
        Parameters.Turn = _mTurnAmount;
        Parameters.OnGround = _isGrounded;
        Parameters.OnLink = Agent.isOnOffMeshLink;

        float runCycle = Mathf.Repeat(Animator.GetCurrentAnimatorStateInfo(0).normalizedTime + RunCycleLegOffset, 1);
        float jumpLeg = (runCycle < KHalf ? 1 : -1) * _mForwardAmount;
        if (_isGrounded)
        {
            Parameters.JumpLeg = jumpLeg;
            Animator.SetFloat("JumpLeg", jumpLeg);
        }

        RaycastHit hit;
        if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), -Vector3.up, out hit))
        {
            _mGroundNormal = hit.normal;
            _groundPosition = hit.point;
            _groundDistance = hit.distance;
        }
        else
        {
            _groundDistance = 0;
        }
        Parameters.GroundDistance = _groundDistance;

        if (_isGrounded && move.magnitude > 0)
        {
            Animator.speed = AnimSpeedMultiplier;
        }
        else
        {
            Animator.speed = 1;
        }
    }

    private void ApplyExtraTurnRotation()
    {
        // help the character turn faster (this is in addition to root rotation in the animation)
        float turnSpeed = Mathf.Lerp(StationaryTurnSpeed, MovingTurnSpeed, _mForwardAmount);
        transform.Rotate(0, _mTurnAmount * turnSpeed * Time.deltaTime, 0);
    }
    public void OnAnimatorMove()
    {
        if (Animator == null) return;

        Animator.ApplyBuiltinRootMotion();

        // we implement this function to override the default root motion.
        // this allows us to modify the positional speed before it's applied.
        if (_isGrounded && Time.deltaTime > 0)
        {
            Vector3 v = (Animator.deltaPosition * MoveSpeedMultiplier) / Time.deltaTime;

            // we preserve the existing y part of the current velocity.
            v.y = Body.velocity.y;
            Body.velocity = v;
        }
    }
    private void CheckGroundStatus()
    {
        var isGrounded = _groundDistance < 0.5f;
        if (isGrounded != _isGrounded)
        {
            OnGroundedStatusChanged(isGrounded);
            _isGrounded = isGrounded;
        }
    }
    private void OnGroundedStatusChanged(bool isGrounded)
    {
        if (!isGrounded)
        {
            _launchPosition = transform.position;
        }
    }
}