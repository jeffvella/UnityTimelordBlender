using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimelordStateBehavior : StateMachineBehaviour
{
    public TimelineAsset TimelineAsset;
    public TimelordMixer Timelines { get; set; }
    public bool IsActive { get; set; }

    public bool FaceWallOnEnter;
    private RaycastHit _hit;
    private float _startTimeSeconds;
    private float _endTimeSeconds;
    private float _duration = 1f;
    private bool _faceCompleted;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Timelines.Play(TimelineAsset);

        animator.SetBool(Parameters.Keys.IsTimelinePlaying, true);

        IsActive = true;
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        IsActive = false;
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (IsActive && !Timelines.IsTimelinePlaying)
        {
            animator.SetBool(Parameters.Keys.IsTimelinePlaying, false);
        }
    }
}


