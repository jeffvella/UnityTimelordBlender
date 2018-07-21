using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Animations;
public class TimelordMixer : MonoBehaviour
{
    private Queue<TimelordDirector> _queue = new Queue<TimelordDirector>();
    public TimelordDirector Current { get; set; }

    private Dictionary<TimelineAsset, TimelordDirector> _directorsByAsset;

    [Header("Timelines")]
    public List<TimelordDirector> Directors;

    [Header("Debug Triggers")]
    public bool _playAll;
    public bool IsTimelinePlaying => _queue.Count > 0 || _directorsByAsset.Values.Any(d => d.IsPlaying);
    public void Start()
    {
        _directorsByAsset = Directors.Where(d => d != null).ToDictionary(k => k.PlayableDirector.playableAsset as TimelineAsset, v => v);
    }
    public void Play(TimelineAsset timeline, bool playImmediately = true, float transitionTime = 0.5f)
    {
        if (timeline == null)
        {
            Debug.Log($"Can't play a null timeline :)");
            return;
        }

        if (!_directorsByAsset.ContainsKey(timeline))
        {
            Debug.Log($"Timelord not found for Timeline {timeline.name}");
            return;
        }

        var director = _directorsByAsset[timeline];

        _queue.Clear();

        if (Current == null)
        {
            _queue.Enqueue(director);
        }
        else if (Current == director && Current.IsPlaying)
        {
            Debug.Log($"Play called on already running timeline {director.name}");
            Current.Seek(0, transitionTime);
        }
        else
        {
            _queue.Enqueue(director);

            if (playImmediately && Current.IsPlaying)
            {
                Debug.Log($"Stopping '{Current.name}' for immediate playing of '{timeline.name}'");
                Current.Stop(transitionTime);
            }
        }
    }

    public void Play(List<TimelineAsset> timelineSequence, bool playImmediately = true, float transitionTime = 0.5f)
    {
        _queue.Clear();

        foreach (var timeline in timelineSequence.Where(t => _directorsByAsset.ContainsKey(t)))
        {
            _queue.Enqueue(_directorsByAsset[timeline]);
        }

        if (playImmediately && Current != null && !Current.IsBlendingOut)
        {
            Current.Stop(transitionTime);
        }
    }
    public void Update()
    {
        if (_playAll)
        {
            _playAll = false;
            _queue = new Queue<TimelordDirector>(Directors);
        }

        if (_queue == null || _queue.Count == 0)
            return;

        if (Current == null || !Current.IsPlaying || Current.IsBlendingOut && _queue.Any())
        {
            Current = _queue.Dequeue();
            Current.Play();
        }
    }
}