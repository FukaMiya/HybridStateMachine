using UnityEngine;
using HybridStateMachine;

// 1. イベントIDの定義
public enum GameEvent
{
    Pause,      // ポーズ
    Resume,     // ポーズ解除
    Attack,     // 攻撃
    GameOver    // ゲームオーバー（外部からの強制通知など）
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
        
        // 例: Score変更用のGameEntryPointを注入して生成
        factory.Register<TitleState>(() => new TitleState(this));
        factory.Register<AttackState>(() => new AttackState(this));

        // 3. ステートマシンの生成
        // 型引数でEnumを指定することで、Push型の機能が使えるようになる
        stateMachine = StateMachine.Create<GameEvent>(factory);

        // 各ステートの取得
        var title = stateMachine.At<TitleState>();
        var inGame = stateMachine.At<InGameState>();
        var pause = stateMachine.At<PauseState>();
        var attack = stateMachine.At<AttackState>();
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
            .Build();

        // C. ハイブリッド型: イベント発生時、さらに条件を満たしていたら遷移
        // Attackイベント発生時に、接地(IsGrounded)していれば InGame -> Attack
        inGame.To(attack)
            .On(GameEvent.Attack)
            .When(() => IsGrounded)
            .Build();

        // 攻撃が終わったら自動で戻る（Pull型）
        attack.To(inGame)
            .When(() => attack.IsAnimationFinished)
            .Build();

        // D. Back（履歴）機能: 直前のステートに戻る
        // Resumeイベントで Pause -> 直前の状態（InGameなど）
        pause.Back()
            .On(GameEvent.Resume)
            .Build();

        // E. AnyState: どの状態からでも遷移
        // GameOverイベントが飛んできたら、リザルトへ
        // ただし、リザルトからの再遷移は不可
        stateMachine.AnyState.To(result)
            .On(GameEvent.GameOver)
            .SetAllowReentry(false)
            .Build();

        // F. コンテキスト渡し: 遷移時にデータを渡す
        // Spaceキーで InGame -> Result (現在のスコアを渡す)
        inGame.To<ResultState, int>(() => Score)
            .When(() => Input.GetKeyDown(KeyCode.Space))
            .Build();

        // リザルトからタイトルへ
        result.To(title)
            .When(() => Input.GetKeyDown(KeyCode.Return))
            .Build();

        // --- 可視化 ---
        // 定義した遷移図をMermaid記法で出力
        Debug.Log(stateMachine.ToMermaidString());

        // --- 初期化 ---
        stateMachine.SetInitialState<TitleState>();
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
    private readonly GameEntryPoint game;

    // 引数付きコンストラクタ（GameEntryPointを受け取る例）
    public TitleState(GameEntryPoint game) 
    {
        this.game = game;
    }

    protected override void OnEnter()
    {
        game.Score = 0; // タイトルに戻ったらスコアリセットの例
        Debug.Log("Enter: Title (Press 'Enter' to Start)");
    }

    protected override void OnUpdate() {}
    protected override void OnExit() {}
}

public class InGameState : State 
{
    protected override void OnEnter() => Debug.Log("Enter: InGame (Press 'P' to Pause, 'Z' to Attack, 'Space' to Result)");
}

public class PauseState : State 
{
    protected override void OnEnter() => Debug.Log("Enter: Pause (Press 'R' to Resume)");
}

public class AttackState : State
{
    public bool IsAnimationFinished = false;
    private float timer = 0f;
    private readonly GameEntryPoint game;

    public AttackState(GameEntryPoint game) 
    {
        this.game = game;
    }

    protected override void OnEnter()
    {
        Debug.Log("Enter: Attack (Duration 1.0s)");
        timer = 0f;
        IsAnimationFinished = false;
    }

    protected override void OnUpdate()
    {
        timer += Time.deltaTime;
        if (timer > 1.0f)
        {
            game.Score += 10; // 攻撃成功でスコア加算の例
            Debug.Log($"Attack!");
            IsAnimationFinished = true;
        }
    }
}

// データを受け取るステート
public class ResultState : State<int>
{
    protected override void OnEnter()
    {
        // 渡されたContextを表示
        Debug.Log($"Enter: Result - Score: {Context}"); 
    }
}