using System.Collections;
using UnityEngine;


public enum StatusLightState
{
    Off,
    Green,
    Red,
    Random
}


public class FakeStatusLight : MonoBehaviour
{
    public GameObject GreenLight;
    public GameObject RedLight;
    public GameObject OffLight;

    public StatusLightState PassColor = StatusLightState.Green;
    public StatusLightState FailColor = StatusLightState.Red;
    public StatusLightState OffColor = StatusLightState.Off;
    public StatusLightState MorseTransmitColor = StatusLightState.Green;

    public KMBombModule Module;

    public bool IsFakeStatusLightReady { get; private set; }
    public bool HasFakeStatusLightFailed { get; private set; }

    private bool _green;
    private bool _off = true;
    private bool _red;

    private bool _flashingStrike;
    private bool _passedForReal;

    public StatusLightState HandlePass(StatusLightState state = StatusLightState.Green)
    {
        _passedForReal = true;
        _flashingStrike = false;
        if (Module != null)
            Module.HandlePass();
        return SetLightColor(state);
    }

    public StatusLightState SetLightColor(StatusLightState color)
    {
        switch (color)
        {
            case StatusLightState.Random:
                color = (StatusLightState) Random.Range(0, 3);
                if (color == StatusLightState.Red) goto case StatusLightState.Red;
                if (color == StatusLightState.Green) goto case StatusLightState.Green;
                goto case StatusLightState.Off;
            case StatusLightState.Red:
                _red = true;
                _green = false;
                _off = false;
                break;
            case StatusLightState.Green:
                _red = false;
                _green = true;
                _off = false;
                break;
            case StatusLightState.Off:
            default:
                _red = false;
                _green = false;
                _off = true;
                break;
        }
        return color;
    }

    public void GetStatusLights(Transform statusLightParent)
    {
        StartCoroutine(GetStatusLight(statusLightParent));
    }

    protected IEnumerator GetStatusLight(Transform statusLightParent)
    {
        for (var i = 0; i < 60; i++)
        {
            var off = statusLightParent.FindDeepChild("Component_LED_OFF");
            var pass = statusLightParent.FindDeepChild("Component_LED_PASS");
            var fail = statusLightParent.FindDeepChild("Component_LED_STRIKE");
            if (off == null || pass == null || fail == null)
            {
                yield return null;
                continue;
            }
            IsFakeStatusLightReady = true;
            OffLight = off.gameObject;
            GreenLight = pass.gameObject;
            RedLight = fail.gameObject;
            yield break;
        }
        HasFakeStatusLightFailed = true;
    }

    public void HandleStrike()
    {
        if (Module == null) return;
        Module.HandleStrike();
        FlashStrike();
    }

    void Update()
    {
        if (_flashingStrike) return;
        if (GreenLight != null)
            GreenLight.SetActive(_green);
        if (OffLight != null)
            OffLight.SetActive(_off);
        if (RedLight != null)
            RedLight.SetActive(_red);
    }

    public void SetPass()
    {
        SetLightColor(PassColor);
    }

    public void SetInActive()
    {
        SetLightColor(OffColor);
    }

    public void SetStrike()
    {
        SetLightColor(FailColor);
    }

    private IEnumerator _flashingStrikeCoRoutine;
    public void FlashStrike()
    {
        if (_passedForReal) return;
        if (!gameObject.activeInHierarchy) return;
        if (_flashingStrikeCoRoutine != null)
            StopCoroutine(_flashingStrikeCoRoutine);
        _flashingStrike = false;
        _flashingStrikeCoRoutine = StrikeFlash(1f);
        StartCoroutine(_flashingStrikeCoRoutine);
    }

    protected IEnumerator StrikeFlash(float blinkTime)
    {
        SetStrike();
        Update();
        _flashingStrike = true;
        yield return new WaitForSeconds(blinkTime);
        SetInActive();
        _flashingStrike = false;
        _flashingStrikeCoRoutine = null;
    }
}
