using UnityEngine;
public class Parameters
{
    private readonly Animator _animator;

    public Parameters(Animator animator)
    {
        _animator = animator;
    }
    public static class Keys
    {
        public static int GroundDistance = Animator.StringToHash(nameof(GroundDistance));
        public static int JumpLeg = Animator.StringToHash(nameof(JumpLeg));
        public static int OnGround = Animator.StringToHash(nameof(OnGround));
        public static int Forward = Animator.StringToHash(nameof(Forward));
        public static int Turn = Animator.StringToHash(nameof(Turn));
        public static int OnLink = Animator.StringToHash(nameof(OnLink));
        public static int IsTimelinePlaying = Animator.StringToHash(nameof(IsTimelinePlaying));
    }
    public float GroundDistance
    {
        get { return _animator.GetFloat(Keys.GroundDistance); }
        set { SetFloat(Keys.GroundDistance, value); }
    }
    public float JumpLeg
    {
        get { return _animator.GetFloat(Keys.JumpLeg); }
        set { SetFloat(Keys.JumpLeg, value); }
    }
    public float Forward
    {
        get { return _animator.GetFloat(Keys.Forward); }
        set { SetDampedFloat(Keys.Forward, value); }
    }
    public float Turn
    {
        get { return _animator.GetFloat(Keys.Turn); }
        set { SetDampedFloat(Keys.Turn, value); }
    }
    public bool OnGround
    {
        get { return _animator.GetBool(Keys.OnGround); }
        set { _animator.SetBool(Keys.OnGround, value); }
    }
    public bool OnLink
    {
        get { return _animator.GetBool(Keys.OnLink); }
        set { _animator.SetBool(Keys.OnLink, value); }
    }
    public void SetDampedFloat(int key, float input)
    {
        _animator.SetFloat(key, input, 0.3f, Time.deltaTime);
    }
    public void SetFloat(int key, float input)
    {
        _animator.SetFloat(key, input);
    }
}