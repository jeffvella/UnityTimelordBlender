using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(PlayableDirector))]
public class TimelordDirector : MonoBehaviour
{
    public PlayableDirector PlayableDirector;

    private AnimationPlayableOutput _output;
    private Playable _originalSourcePlayable;
    private Playable _clone;
    private AnimationMixerPlayable _mixer;
    private int _cloneIndex;
    private int _originalIndex;
    private float _decreasingWeight;
    private float _increasingWeight;

    void Awake()
    {
        if (PlayableDirector == null)
        {
            PlayableDirector = GetComponent<PlayableDirector>();
        }
    }
    private void BuildOutput()
    {
        PlayableDirector.Evaluate();

        if (PlayableDirector.playableGraph.IsValid())
        {
            _outputTrackIndex = 0;
            _trackAsset = (PlayableDirector.playableAsset as TimelineAsset)?.GetOutputTrack(_outputTrackIndex);
            _originalOutput = (AnimationPlayableOutput)PlayableDirector.playableGraph.GetOutputByType<AnimationPlayableOutput>(_outputTrackIndex);
            _originalSourcePlayable = _originalOutput.GetSourcePlayable();
            _clone = PlayableDirector.playableAsset.CreatePlayable(PlayableDirector.playableGraph, PlayableDirector.gameObject);
            _mixer = AnimationMixerPlayable.Create(PlayableDirector.playableGraph, 2);
            _cloneIndex = _mixer.AddInput(_clone, 0);
            _originalIndex = _mixer.AddInput(_originalSourcePlayable, 0, 1f);

            if (_originalOutput.IsOutputValid() && _originalOutput.GetTarget() != null)
            {
                _output = AnimationPlayableOutput.Create(PlayableDirector.playableGraph, "OverridedDirectorOutput" + GetInstanceID(), _originalOutput.GetTarget());
                _output.SetSourcePlayable(_mixer);
                _output.SetSourceOutputPort(_originalOutput.GetSourceOutputPort());
                _output.SetWeight(1f);
                _originalOutput.SetTarget(null);
            }
            else
            {
                Debug.Log("Original Director Output is invalid");
            }
        }
    }

    private TrackAsset _trackAsset;

    private double _outTime = -1;

    public bool IsPlaying => PlayableDirector.state == PlayState.Playing;

    public BlendType CurrentBlend { get; set; }

    public bool IsBlending => CurrentBlend != BlendType.None;

    public bool IsBlendingOut => CurrentBlend == BlendType.Out;

    public bool IsBlendingIn => CurrentBlend == BlendType.In;

    public double TimeRemaining => PlayableDirector.time - PlayableDirector.duration;

    public string Name => PlayableDirector.name;

    public enum BlendType
    {
        None = 0,
        In,
        Out,
        Seek,
    }

    private Action _scheduledBlendOutCallback;
    private float _scheduledBlendOutDuration;
    private AnimationPlayableOutput _originalOutput;
    private int _outputTrackIndex;
    private Stopwatch _sw = new Stopwatch();
    private static bool _abortBlendIn;


    void Update()
    {
        if (IsPlaying && CurrentBlend == BlendType.None)
        {
            if (_outTime >= 0 && PlayableDirector.time >= _outTime)
            {
                CurrentBlend = BlendType.Out;

                //Debug.Log($"{name}: Starting Scheduled Blend-Out at {PlayableDirector.time:N2}");
                _sw.Restart();

                StartCoroutine(BlendOut(PlayableDirector, _output, _scheduledBlendOutDuration, _output.GetWeight(), () =>
                {
                    _sw.Stop();
                    //Debug.Log($"{name}: Finished Scheduled Blend-Out after {_sw.Elapsed.TotalSeconds:N3}ms");
                    _scheduledBlendOutCallback?.Invoke();
                    _scheduledBlendOutCallback = null;
                    CurrentBlend = BlendType.None;
                }));
            }
        }
    }

    public void Play(float blendInDuration = 0.5f, float blendOutDuration = 0.5f, float startTime = -1, float endTime = -1, Action onBlendInFinished = null, Action onFinished = null)
    {

        //Debug.Log($"{name}: Play. CurrentBlend={CurrentBlend}");

        if (CurrentBlend == BlendType.None)
        {
            CurrentBlend = BlendType.In;

            if (!_output.IsOutputValid())
            {
                //Debug.Log($"{name}: Rebuilding output");
                PlayableDirector.RebuildGraph();
                BuildOutput();
            }

            if (_output.IsOutputValid())
            {
                _scheduledBlendOutCallback = onFinished;
                _scheduledBlendOutDuration = blendOutDuration;

                _outTime = (endTime < 0
                    ? _trackAsset.start + _trackAsset.duration - blendOutDuration
                    : Math.Max(0, endTime - blendInDuration)) - Time.deltaTime;

                //Debug.Log($"{name}: scheduling OUT: {_outTime:N2}");

                if (blendInDuration == 0f)
                {
                    PlayableDirector.Play();
                }
                else
                {
                    StartCoroutine(BlendIn(PlayableDirector, _output, blendInDuration, startTime, () =>
                    {
                        onBlendInFinished?.Invoke();
                        _scheduledBlendOutCallback = null;
                        CurrentBlend = BlendType.None;
                    }));
                }
            }
            else
            {
                //Debug.Log($"{nameof(TimelordDirector)}.{nameof(Play)}() failed because the graph was invalid.");
            }
        }
        else
        {
            //Debug.Log($"{nameof(TimelordDirector)}.{nameof(Play)}() was called while already blending ({CurrentBlend}), and was ignored.");
        }
    }

    public void Stop(float blendDuration = 0.5f, Action onFinished = null)
    {
        Debug.Log($"{name}: Stop. CurrentBlend={CurrentBlend}");

        if (blendDuration <= 0f)
        {
            PlayableDirector.Pause();
        }

        if (CurrentBlend != BlendType.None && CurrentBlend != BlendType.In)
        {
            //Debug.Log($"{nameof(TimelordDirector)}.{nameof(Stop)}() was called while already blending ({CurrentBlend}), and was ignored.");
            return;
        }

        if (CurrentBlend == BlendType.In)
        {
            _abortBlendIn = true;
        }

        CurrentBlend = BlendType.Out;

        if (!_output.IsOutputValid())
        {
            Debug.Log($"{name}: Rebuilding output");
            PlayableDirector.RebuildGraph();
            BuildOutput();
        }

        if (_output.IsOutputValid())
        {
            StartCoroutine(BlendOut(PlayableDirector, _output, blendDuration, _output.GetWeight(), () =>
            {
                onFinished?.Invoke();
                CurrentBlend = BlendType.None;
            }));
        }
        else
        {
            //Debug.Log($"{nameof(TimelordDirector)}.{nameof(Stop)}() failed because the graph was invalid.");
        }


    }

    public void Seek(float toTime, float blendDuration, Action onFinished = null)
    {
        //Debug.Log($"{name}: Seek. CurrentBlend={CurrentBlend} to={toTime}");

        if (CurrentBlend == BlendType.None)
        {
            CurrentBlend = BlendType.Seek;

            if (_output.IsOutputValid())
            {
                StartCoroutine(SeekBlend(blendDuration, toTime, () =>
                {
                    onFinished?.Invoke();
                    CurrentBlend = BlendType.None;
                }));
            }
            else
            {
                //Debug.Log($"{nameof(TimelordDirector)}.{nameof(Stop)}() failed because the graph was invalid.");
            }
        }
    }

    private IEnumerator BlendOut(PlayableDirector director, AnimationPlayableOutput output, float blendTime, float fromWeight = 1f, Action onFinished = null)
    {

        float t = blendTime - blendTime * fromWeight;

        //Debug.Log($"{name}: Started blend out from {fromWeight} weight, oW={output.GetWeight()}, t={t}");

        while (t < blendTime)
        {
            var weight = 1 - Mathf.Clamp01(t / blendTime);

            if (!output.IsOutputValid())
            {
                //Debug.Log($"{name}: BlendOut has an invalid output graph");
                break;
            }

            //Debug.Log($"{name}: BlendOut - t:{t:N2}, w={weight:N2}, dT={director.time:N2}/{director.duration:N2}");
            output.SetWeight(weight);

            yield return null;
            t += Time.deltaTime;
        }

        if (output.IsOutputValid())
        {
            output.SetWeight(0);
        }

        onFinished?.Invoke();

        if (director.isActiveAndEnabled)
            director.Pause();
    }

    private IEnumerator BlendIn(PlayableDirector director, AnimationPlayableOutput output, float blendTime, float startTime = -1, Action onFinished = null)
    {
        director.time = startTime > 0 ? startTime : 0;
        director.Play();
        output.SetWeight(0);
        _abortBlendIn = false;

        float t = 0;
        while (t < blendTime)
        {
            if (_abortBlendIn)
            {
                //Debug.Log($"{name}: Aborted Blend in");
                _abortBlendIn = false;
                break;
            }

            var weight = Mathf.Clamp01(t / blendTime);

            output.SetWeight(weight);

            //Debug.Log($"{name}: BlendIn - t:{t:N2}, w={weight:N2}, dT={director.time:N2}");

            yield return null;
            t += Time.deltaTime;
        }

        output.SetWeight(1);
        onFinished?.Invoke();
    }

    private IEnumerator SeekBlend(float blendTime, float startTime = -1, Action onFinished = null)
    {
        float t = 0;

        // Set it to play exactly what the main output is currently playing.
        _clone.SetTime(PlayableDirector.time);
        _clone.Play();

        //Debug.Log($"{name}: CrossFade Start - OriginalTime: {PlayableDirector.time:N4}, w={_mixer.GetInputWeight(_originalIndex)} | CloneTime {_clone.GetTime():N4}, w={_mixer.GetInputWeight(_cloneIndex)} | oW={_output.GetWeight()}");

        // todo, blend main weight if nessesary e.g seeking half-way through a blend-in/out
        _output.SetWeight(1);

        // Let the clone take over control.
        _mixer.SetInputWeight(_cloneIndex, 1f);
        _mixer.SetInputWeight(_originalIndex, 0f);

        // Normally when time changes (and play() or evaluate() are called) it will reposition
        // the transform based on what should have happened by the new point in time (relative to the timeline start).
        // Which is not desirable! Instead we want everything to continue from its current position.
        // This issue can be circumvented by changing a playable while inactive.
        //_output.SetWeight(0);
        PlayableDirector.time = startTime;
        PlayableDirector.Play();
        PlayableDirector.Evaluate();

        //Debug.Log($"AnimationTransform: 1) {start} 2) {cur} Dist: {Vector3.Distance(start, cur)}");

        //float t = 0f;
        // Blend from the 
        while (t < blendTime)
        {

            var normalizedTime = Mathf.Clamp01(t / blendTime);
            _decreasingWeight = 1 - normalizedTime;
            _increasingWeight = normalizedTime;

            _mixer.SetInputWeight(_cloneIndex, _decreasingWeight);
            _mixer.SetInputWeight(_originalIndex, _increasingWeight);

            //Debug.Log($"{name}: Seek - CloneTime: {_clone.GetTime():N2}, w={_decreasingWeight:N2} | OriginalTime: {PlayableDirector.time:N2}, w={_increasingWeight:N2}");

            yield return null;
            t += Time.deltaTime;
        }

        _mixer.SetInputWeight(_cloneIndex, 0);
        _mixer.SetInputWeight(_originalIndex, 1f);

        _clone.Pause();



        onFinished?.Invoke();
        //Debug.Log("CrossFade Finished");
    }

}