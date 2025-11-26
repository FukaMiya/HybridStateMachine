using UnityEngine;
using FukaMiya.Utils;

public class TitleState : State
{
    public override void OnEnter()
    {
        Debug.Log("Entered Title State");
    }
}

public class InGameState : State
{
    public override void OnEnter()
    {
        Debug.Log("Entered InGame State");
    }
}

public class ResultState : State
{
    public override void OnEnter()
    {
        Debug.Log("Entered Result State");
    }
}

public class SettingState : State
{
    public override void OnEnter()
    {
        Debug.Log("Entered Setting State");
    }
}