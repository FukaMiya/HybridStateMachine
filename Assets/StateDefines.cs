using UnityEngine;
using FukaMiya.Utils;

public sealed class TitleState : State
{
    protected override void OnEnter()
    {
        Debug.Log("Entered Title State");
    }
}

public sealed class InGameState : State
{
    protected override void OnEnter()
    {
        Debug.Log("Entered InGame State");
    }
}

public sealed class ResultState : State<int>
{
    protected override void OnEnter()
    {
        Debug.Log($"Entered Result State with Score: {Context}");
    }
}

public sealed class SettingState : State
{
    protected override void OnEnter()
    {
        Debug.Log("Entered Setting State");
    }
}

public sealed class SecretState : State
{
    protected override void OnEnter()
    {
        Debug.Log($"Entered Secret State");
    }
}