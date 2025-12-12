using System;
using UnityEngine;
using FukaMiya.Utils;

// 1. イベントIDの定義（プロジェクトで統一したEnumを使うのがおすすめ）
public enum GameEvent
{
    Pause,      // ポーズ
    Resume,     // ポーズ解除
    Attack,     // 攻撃
    GameOver    // ゲームオーバー（外部からの強制通知など）
}

enum Test
{
    A, B, C
}

public class GameEntryPoint : MonoBehaviour
{
    // インターフェースで保持（Pull/Push両方使える型）
    private IPushAndPullStateMachine stateMachine;

    // デモ用のパラメータ
    public int Score = 0;
    public bool IsGrounded = true;

    void Start()
    {
        // 2. ファクトリのセットアップ
        // 引数なしコンストラクタのStateは自動生成されるので登録不要。
        // 引数が必要なStateだけ手動で登録する。
        var factory = new StateFactory();
        
        // 例: 初期スコアを注入して生成
        factory.Register<ResultState>(() => new ResultState(score: 0)); 

        // 3. ステートマシンの生成
        // 型引数でEnumを指定することで、APIの整合性を保つ（内部的にはint管理）
        stateMachine = StateMachine.Create<GameEvent>(factory);

        // 各ステートの取得
        var title = stateMachine.At<TitleState>();
        var inGame = stateMachine.At<InGameState>();
        var pause = stateMachine.At<PauseState>();
        var attack = stateMachine.At<AttackState>() as AttackState;
        var result = stateMachine.At<ResultState>();

        // --- 遷移定義 ---

        // A. Pull型（ポーリング）: 条件を満たしたら遷移
        // Enterキーで Title -> InGame
        title.To(inGame)
            .When(() => Input.GetKeyDown(KeyCode.Return))
            .Build();

        // B. Push型（イベント駆動）: イベントが発火したら即座に遷移
        // Pauseイベントで InGame -> Pause
        inGame.To(pause)
            .On(GameEvent.Pause)
            .Always(); // 条件なし（イベントのみ）の場合はAlways必須

        // C. ハイブリッド型: イベント発生時、さらに条件を満たしていたら遷移
        // Attackイベント発生時に、接地(IsGrounded)していれば InGame -> Attack
        inGame.To(attack)
            .On(GameEvent.Attack)
            .When(() => IsGrounded)
            .Build();

        // 攻撃が終わったら自動で戻る（Pull型）
        attack.To(inGame)
            .When(() => attack.Context.IsAnimationFinished) // ※Context例として自分自身を参照
            .Build();

        // D. Back（履歴）機能: 直前のステートに戻る
        // Resumeイベントで Pause -> 直前の状態（InGameなど）
        pause.Back()
            .On(GameEvent.Resume)
            .Always();

        // E. AnyState: どの状態からでも遷移
        // GameOverイベントが飛んできたら、問答無用でリザルトへ
        stateMachine.AnyState.To(result)
            .On(GameEvent.GameOver)
            .Always();

        // F. コンテキスト渡し: 遷移時にデータを渡す
        // Spaceキーで InGame -> Result (現在のスコアを渡す)
        inGame.To<ResultState, int>(() => Score)
            .When(() => Input.GetKeyDown(KeyCode.Space))
            .Build();

        // リザルトからタイトルへ（再入を許可しない設定例）
        result.To(title)
            .When(() => Input.GetKeyDown(KeyCode.Return))
            .SetAllowReentry(false)
            .Build();

        // --- 初期化 ---
        stateMachine.SetInitialState<TitleState>();

        // --- 可視化 ---
        // 定義した遷移図をMermaid記法でコンソールに出力
        Debug.Log(stateMachine.ToMermaidString());
    }

    void Update()
    {
        // 4. 更新処理 (Pull型の監視)
        stateMachine.Update();

        // 5. イベント発火 (Push型のトリガー)
        // 拡張メソッドのおかげでEnumをそのまま渡せる
        if (Input.GetKeyDown(KeyCode.P))
        {
            stateMachine.Fire(GameEvent.Pause);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            stateMachine.Fire(GameEvent.Resume);
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            stateMachine.Fire(GameEvent.Attack);
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            stateMachine.Fire(GameEvent.GameOver);
        }
    }
}

// --- ステート定義例 ---

public class TitleState : State 
{
    protected override void OnEnter() => Debug.Log("Enter: Title");
}

public class InGameState : State 
{
    protected override void OnEnter() => Debug.Log("Enter: InGame (Press 'P' to Pause, 'Z' to Attack, 'Space' to Result)");
    protected override void OnUpdate() 
    {
        // ステート内で入力監視も可能
    }
}

public class PauseState : State 
{
    protected override void OnEnter() => Debug.Log("Enter: Pause (Press 'R' to Resume)");
}

public class AttackState : State<AttackState> // 自分自身をContextにするパターン
{
    public bool IsAnimationFinished = false;
    private float timer = 0f;

    protected override void OnEnter()
    {
        Debug.Log("Enter: Attack (Duration 1.0s)");
        timer = 0f;
        IsAnimationFinished = false;
        SetContextProvider(() => this); // 自分をContextとしてセット
    }

    protected override void OnUpdate()
    {
        timer += Time.deltaTime;
        if (timer > 1.0f) IsAnimationFinished = true;
    }
}

// データを受け取るステート
public class ResultState : State<int>
{
    private readonly int defaultScore;

    // 引数付きコンストラクタ（Factoryで生成される）
    public ResultState(int score)
    {
        this.defaultScore = score;
    }
    // 自動生成用（念のため用意する場合）
    public ResultState() : this(0) { }

    protected override void OnEnter()
    {
        // 渡されたContextを表示
        Debug.Log($"Enter: Result - Score: {Context}"); 
    }
}