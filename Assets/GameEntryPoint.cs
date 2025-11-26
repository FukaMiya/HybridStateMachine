using UnityEngine;
using FukaMiya.Utils;

public class GameEntryPoint : MonoBehaviour
{
    private StateMachine stateMachine;

    void Start()
    {
        stateMachine = new StateMachine();
        var titleState = stateMachine.At<TitleState>();
        var inGameState = stateMachine.At<InGameState>();
        var resultState = stateMachine.At<ResultState>();
        var settingState = stateMachine.At<SettingState>();

        // Whenに直接条件を渡す
        titleState.To<InGameState>()
            .When(Condition.Any(
                () => Input.GetKeyDown(KeyCode.Return),
                () => Input.GetMouseButtonDown(0)))
            .Build();

        inGameState
            .To<SettingState>()
            .When(() => Input.GetKeyDown(KeyCode.Escape))
            .Build();

        // 複雑な条件
        inGameState.To<ResultState>()
            .When(Condition.All(
                Condition.Any(
                    () => Input.GetKey(KeyCode.LeftShift),
                    () => Input.GetKey(KeyCode.RightShift)
                ),
                () => Input.GetKey(KeyCode.Alpha1),
                () => Input.GetKey(KeyCode.Alpha2),
                () => Input.GetKeyDown(KeyCode.Alpha3)))
            .Build();

        settingState.To<InGameState>()
            .When(() => Input.GetKeyDown(KeyCode.Escape))
            .Build();

        inGameState.To<ResultState>()
            .When(() => Input.GetKeyDown(KeyCode.Space))
            .Build();

        resultState.To<InGameState>()
            .When(() => Input.GetKeyDown(KeyCode.Return))
            .Build();

        settingState.To<TitleState>()
            .When(() => Input.GetKeyDown(KeyCode.Return))
            .Build();

        stateMachine.SetInitialState<TitleState>();
    }

    void Update()
    {
        stateMachine.Update();
    }
}
