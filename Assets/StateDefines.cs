using UnityEngine;
using FukaMiya.Utils;

public sealed class TitleState : State
{
    public override void OnEnter()
    {
        Debug.Log("Entered Title State");
    }
}

public sealed class InGameState : State
{
    public override void OnEnter()
    {
        Debug.Log("Entered InGame State");
    }
}

public sealed class ResultState : State
{
    public override void OnEnter()
    {
        Debug.Log("Entered Result State");
    }
}

public sealed class SettingState : State
{
    public override void OnEnter()
    {
        Debug.Log("Entered Setting State");
    }
}

public sealed class SecretState : State
{
    public override void OnEnter()
    {
        Debug.Log("Entered Secret State");
    }
}