using UnityEngine;
using UnityEngine.InputSystem;

namespace OrZero
{
    /// <summary>
    /// Game シーンの進行を enum のステートで管理するクラス（SPEC §1.2）。
    /// ステートを持つのはこのクラスだけで、UI は表示と入力の通知だけを受け持つ。
    /// いまは Playing（出題・回答）・Miss（不正解）・TimeUp（時間切れ）を使う。
    /// 回答はボタン（マウス）とキーボード（0キー／テンキーの0＝数字、Oキー＝英字）の両方で受け付ける
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        // ===== 調整値・参照（Inspector で設定） =====
        [SerializeField] private GameBalanceData balanceData;    // ゲーム全体の調整値
        [SerializeField] private GlyphView glyphView;            // 画面中央の出題文字
        [SerializeField] private AnswerButton[] answerButtons;   // 回答ボタン（英字用と数字用を1つずつ）
        [SerializeField] private HudView hudView;                // 画面上部の SCORE・TIME・COMBO

        // ===== 実行時の状態（確認用に Inspector へ表示） =====
        [SerializeField] private GameState currentState = GameState.Playing;      // 現在のステート
        [SerializeField] private AnswerPicker answerPicker = new AnswerPicker();  // 答えの抽選（同じ答えの連続上限つき）
        [SerializeField] private GameTimer gameTimer = new GameTimer();           // 残り時間（秒）
        [SerializeField] private GlyphType currentAnswer = GlyphType.LetterO;     // いま出している問題の答え
        [SerializeField] private float questionElapsedSeconds;                    // いまの問題を出してからの経過時間（秒）。スピード加点に使う
        [SerializeField] private int correctCount;                                // 正解数（ミスで即終了のため、コンボ数と同じ）
        [SerializeField] private int score;                                       // 現在のスコア（点）

        /// <summary>
        /// 起動時に Inspector の設定漏れを確かめる。漏れがあればこのコンポーネントを止める
        /// （止めると OnEnable・Start・Update も呼ばれないので、以降のエラーが連鎖しない）
        /// </summary>
        private void Awake()
        {
            if (!HasValidSettings())
            {
                enabled = false;
            }
        }

        /// <summary>
        /// 有効になったとき、回答ボタンの通知を受け取り始める
        /// </summary>
        private void OnEnable()
        {
            // ローカル変数は関数の先頭で宣言する
            int i;   // ループ用の添字

            for (i = 0; i < answerButtons.Length; i++)
            {
                answerButtons[i].Pressed += HandleAnswerPressed;
            }
        }

        /// <summary>
        /// 無効になったとき、回答ボタンの通知の受け取りをやめる
        /// </summary>
        private void OnDisable()
        {
            // ローカル変数は関数の先頭で宣言する
            int i;   // ループ用の添字

            for (i = 0; i < answerButtons.Length; i++)
            {
                answerButtons[i].Pressed -= HandleAnswerPressed;
            }
        }

        /// <summary>
        /// 最初のフレームの前に、抽選・残り時間・スコアを準備して出題を始める
        /// </summary>
        private void Start()
        {
            // 実行時の状態は、Inspector に残った値に左右されないようにここで必ず設定し直す
            correctCount = 0;
            score = 0;
            answerPicker.Initialize(balanceData.MaxSameAnswerStreak, new System.Random());
            gameTimer.Reset(balanceData.StartSeconds);

            // HUD の初期表示
            hudView.ShowScore(score);
            hudView.ShowCombo(correctCount);
            hudView.ShowTime(gameTimer.RemainingSeconds);

            // カウントダウン（T9）ができるまでは、すぐに出題から始める
            ChangeState(GameState.Playing);
        }

        /// <summary>
        /// 毎フレーム、現在のステートに応じた更新を行う
        /// </summary>
        private void Update()
        {
            switch (currentState)
            {
                case GameState.Playing:
                    UpdatePlaying();
                    break;

                default:
                    // Playing 以外のステートには、まだ毎フレームの処理がない
                    break;
            }
        }

        /// <summary>
        /// Playing 中の毎フレームの処理
        /// （残り時間と回答時間を進め、0 になったら時間切れにする。まだ時間があればキーボードの回答を読む）
        /// </summary>
        private void UpdatePlaying()
        {
            gameTimer.Tick(Time.deltaTime);
            questionElapsedSeconds += Time.deltaTime;
            hudView.ShowTime(gameTimer.RemainingSeconds);

            if (gameTimer.IsTimeUp)
            {
                ChangeState(GameState.TimeUp);
                return;
            }

            ReadKeyboardAnswer();
        }

        /// <summary>
        /// キーボードでの回答を読む（0キー・テンキーの0＝数字、Oキー＝英字。SPEC §1.1）。
        /// 同じフレームで両方押された場合は、どちらとも決められないので無視する
        /// </summary>
        private void ReadKeyboardAnswer()
        {
            // ローカル変数は関数の先頭で宣言する
            Keyboard keyboard;     // 接続中のキーボード（無ければ null）
            bool zeroPressed;      // このフレームで数字側のキー（0／テンキーの0）が押されたか
            bool letterPressed;    // このフレームで英字側のキー（O）が押されたか

            keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            // キーは物理的な位置で判定される（日本語配列でも 0 と O の位置は同じ）
            zeroPressed = keyboard.digit0Key.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame;
            letterPressed = keyboard.oKey.wasPressedThisFrame;

            // どちらも押されていない、または両方押されたときは回答にしない
            if (zeroPressed == letterPressed)
            {
                return;
            }

            HandleAnswerPressed(zeroPressed ? GlyphType.DigitZero : GlyphType.LetterO);
        }

        /// <summary>
        /// ステートを切り替え、そのステートに入ったときの処理を行う
        /// </summary>
        /// <param name="nextState">次のステート</param>
        private void ChangeState(GameState nextState)
        {
            currentState = nextState;
            EnterState(nextState);
        }

        /// <summary>
        /// ステートに入ったときの処理
        /// </summary>
        /// <param name="state">入ったステート</param>
        private void EnterState(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    ShowNextQuestion();
                    break;

                case GameState.Miss:
                    // リザルト画面（T11）ができるまでは Console に出すだけ
                    Debug.Log($"GAME OVER（仮）: 正解は {currentAnswer}／正解数 {correctCount}／スコア {score}", this);
                    break;

                case GameState.TimeUp:
                    // リザルト画面（T11）ができるまでは Console に出すだけ
                    Debug.Log($"TIME UP（仮）: 正解数 {correctCount}／スコア {score}", this);
                    break;

                default:
                    // ほかのステートの処理は、それぞれのタスク（T9・T14）で足す
                    break;
            }
        }

        /// <summary>
        /// 回答が入力されたときの処理（AnswerButton の Pressed と、キーボードの読み取りから呼ばれる）
        /// </summary>
        /// <param name="pressedAnswer">入力された答え</param>
        private void HandleAnswerPressed(GlyphType pressedAnswer)
        {
            // 回答を受け付けるのは Playing 中だけ（Miss・TimeUp になった後の入力は無視する）
            if (currentState != GameState.Playing)
            {
                return;
            }

            // 文字が切り替わった直後の入力は無視する（SPEC §1.4 ルール6）。
            // ダブルクリックや、マウスとキーの同時入力が、次の問題への回答にならないようにするため。
            // フラグは使わず、出題からの経過時間で判定する
            if (questionElapsedSeconds < balanceData.InputLockSeconds)
            {
                return;
            }

            if (pressedAnswer == currentAnswer)
            {
                HandleCorrect(questionElapsedSeconds);
            }
            else
            {
                // 不正解: 1回で即ゲームオーバー
                ChangeState(GameState.Miss);
            }
        }

        /// <summary>
        /// 正解したときの処理（得点加算・正解数の更新・時間延長の判定・HUD の更新・次の問題）
        /// </summary>
        /// <param name="answerSeconds">文字が表示されてから押すまでの秒数</param>
        private void HandleCorrect(float answerSeconds)
        {
            // ローカル変数は関数の先頭で宣言する
            int addPoint;   // 今回の獲得点

            addPoint = ScoreCalculator.CalculatePoint(
                balanceData.BasePoint, balanceData.SpeedBonusMax,
                balanceData.SpeedBonusWindowSeconds, answerSeconds);

            score += addPoint;
            correctCount++;

            // 決まった問題数ごとに時間を延長する（間隔と秒数は Inspector で調整）
            if (correctCount % balanceData.ExtendInterval == 0)
            {
                gameTimer.Extend(balanceData.ExtendSeconds, balanceData.MaxSeconds);
            }

            // 延長した時間もすぐ見えるように、TIME も一緒に更新する
            hudView.ShowScore(score);
            hudView.ShowCombo(correctCount);
            hudView.ShowTime(gameTimer.RemainingSeconds);

            ShowNextQuestion();
        }

        /// <summary>
        /// 次の問題を抽選して表示し、回答時間の計測をやり直す
        /// </summary>
        private void ShowNextQuestion()
        {
            currentAnswer = answerPicker.PickNext();
            glyphView.Show(currentAnswer);
            questionElapsedSeconds = 0f;
        }

        /// <summary>
        /// Inspector の設定漏れ・設定ミスがないか確かめる
        /// </summary>
        /// <returns>問題がなければ true</returns>
        private bool HasValidSettings()
        {
            // ローカル変数は関数の先頭で宣言する
            int letterButtonCount;   // 英字用のボタンの数
            int digitButtonCount;    // 数字用のボタンの数
            int i;                   // ループ用の添字

            if (balanceData == null || glyphView == null || answerButtons == null || hudView == null)
            {
                Debug.LogError("GameFlowController: balanceData・glyphView・answerButtons・hudView を Inspector で設定してください", this);
                return false;
            }

            // 英字用と数字用のボタンが1つずつそろっているか（左右の入れ替えミスで、答えられない問題が出ないように）
            letterButtonCount = 0;
            digitButtonCount = 0;
            for (i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] == null)
                {
                    Debug.LogError($"GameFlowController: answerButtons の {i} 番目が未設定です", this);
                    return false;
                }

                if (answerButtons[i].AnswerType == GlyphType.LetterO)
                {
                    letterButtonCount++;
                }
                else if (answerButtons[i].AnswerType == GlyphType.DigitZero)
                {
                    digitButtonCount++;
                }
            }

            if (letterButtonCount != 1 || digitButtonCount != 1)
            {
                Debug.LogError($"GameFlowController: 英字用と数字用のボタンを1つずつ設定してください（いまは英字 {letterButtonCount}・数字 {digitButtonCount}。AnswerButton の answerType を確認）", this);
                return false;
            }

            return true;
        }
    }
}
