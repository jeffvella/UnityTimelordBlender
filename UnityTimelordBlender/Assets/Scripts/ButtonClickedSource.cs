using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

public class ButtonClickedSource : MonoBehaviour
{
    public TimelineAsset Timeline;

    public delegate void ButtonClickedDelegate(TimelineAsset timeline);
    public static event ButtonClickedDelegate ButtonClickedEvent;    
    public void OnClick()
    {
        ButtonClickedEvent?.Invoke(Timeline);
    }
}
