using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.SceneManagement;

namespace OrZero
{
    /// <summary>
    /// Game シーンの進行を enum のステートで管理するクラス（SPEC §1.2）。
    /// ステートを持つのはこのクラスだけで、UI は表示と入力の通知だけを受け持つ。
    /// いまは Playing（出題・回答）・Miss（不正解）・TimeUp（時間切れ）を使う。
    /// 回答はボタン（マウス）とキーボード（0キー／テンキーの0＝数字、Oキー＝英字）の両方で受け付ける。
    /// 出題の見た目は、起動時に Resources から読み込んだスタイルのうち、今の正解数で解禁されているものから毎問ランダムに選ぶ（SPEC §1.4）
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        // ===== 調整値・参照（Inspector で設定） =====
        [SerializeField] private GameBalanceData balanceData;    // ゲーム全体の調整値
        [SerializeField] private GlyphView glyphView;            // 画面中央の出題文字
        [SerializeField] private AnswerButton[] answerButtons;   // 回答ボタン（英字用と数字用を1つずつ）
        [SerializeField] private HudView hudView;                // 画面上部の SCORE・TIME・COMBO
        [SerializeField] private string styleResourcesFolder = "GlyphStyles";   // スタイルを置く Resources の中のフォルダ名（Assets/OrZero/Resources/GlyphStyles）
        [SerializeField] private TMP_Text txtMessege;   // 仮のゲームオーバー表示（GAME OVER／TIME UP）。リザルト画面（T13）で置き換える
        [SerializeField] private TMP_Text txtReturn;    // 仮のやり直しの案内（ENTER TO RETRY）。リザルト画面（T13）で置き換える

        // ===== 実行時の状態（確認用に Inspector へ表示） =====
        [SerializeField] private GameState currentState = GameState.Playing;      // 現在のステート
        [SerializeField] private AnswerPicker answerPicker = new AnswerPicker();  // 答えの抽選（同じ答えの連続上限つき）
        [SerializeField] private GameTimer gameTimer = new GameTimer();           // 残り時間（秒）
        [SerializeField] private List<GlyphStyleData> loadedStyles = new List<GlyphStyleData>();   // 起動時に読み込んだスタイル（出題に使うもの）
        [SerializeField] private GlyphType currentAnswer = GlyphType.LetterO;     // いま出している問題の答え
        [SerializeField] private string currentStyleName = "";                    // いま出している問題のスタイル名
        [SerializeField] private float questionElapsedSeconds;                    // いまの問題を出してからの経過時間（秒）。スピード加点に使う
        [SerializeField] private int correctCount;                                // 正解数（ミスで即終了のため、コンボ数と同じ）
        [SerializeField] private int score;                                       // 現在のスコア（点）

        // 出題内容を作るもの（答えとスタイルを独立に抽選する）。Unity が保存できない型なので SerializeField にしない
        private QuestionGenerator questionGenerator;

        /// <summary>
        /// 起動時に Inspector の設定漏れを確かめ、出題スタイルを読み込む。どちらかに問題があればこのコンポーネントを止める
        /// （止めると OnEnable・Start・Update も呼ばれないので、以降のエラーが連鎖しない）
        /// </summary>
        private void Awake()
        {
            if (!HasValidSettings() || !LoadStyles())
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
            // ローカル変数は関数の先頭で宣言する
            System.Random random;   // 答え・スタイル・大きさ・装飾の抽選に使う乱数

            // 実行時の状態は、Inspector に残った値に左右されないようにここで必ず設定し直す
            correctCount = 0;
            score = 0;
            random = new System.Random();
            answerPicker.Initialize(balanceData.MaxSameAnswerStreak, random);
            questionGenerator = new QuestionGenerator(answerPicker, loadedStyles, random);
            gameTimer.Reset(balanceData.StartSeconds);

            // HUD の初期表示
            hudView.ShowScore(score);
            hudView.ShowCombo(correctCount);
            hudView.ShowTime(gameTimer.RemainingSeconds);

            txtMessege.text = "";
            txtReturn.text = "";

            // カウントダウン（T11）ができるまでは、すぐに出題から始める
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

                case GameState.Miss:
                case GameState.TimeUp:
                    UpdateRetryWait();
                    break;

                default:
                    // ほかのステート（Countdown・Paused・Result）の毎フレームの処理は、それぞれのタスク（T11・T16・T13）で足す
                    break;
            }

            ReadQuitKey();
        }

        /// <summary>
        /// ゲームオーバー・時間切れのあと、Enter キー（テンキーの Enter も）で最初からやり直す。
        /// 仮の操作で、リザルト画面の RETRY（T13）ができたら置き換える
        /// </summary>
        private void UpdateRetryWait()
        {
            // ローカル変数は関数の先頭で宣言する
            Keyboard keyboard;   // 接続中のキーボード（無ければ null）

            // ほかのキー入力と同じく Input System で読む（古い Input は、Input System だけの設定だと例外になるため）
            keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                // 今のシーンを読み直してやり直す（Game シーンが Build Profiles の Scene List に入っている必要がある）
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        /// <summary>
        /// Esc キーでアプリを終了する（どのステートでも。Editor では何も起きない）。
        /// 仮の操作で、SPEC では Esc はポーズ（T16）
        /// </summary>
        private void ReadQuitKey()
        {
            // ローカル変数は関数の先頭で宣言する
            Keyboard keyboard;   // 接続中のキーボード（無ければ null）

            keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Application.Quit();
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
                    // リザルト画面（T13）ができるまでは、Console と仮の表示に出すだけ
                    Debug.Log($"GAME OVER（仮）: 正解は {currentAnswer}（スタイル「{currentStyleName}」）／正解数 {correctCount}／スコア {score}", this);
                    txtMessege.text = "GAME OVER";
                    txtReturn.text = "ENTER TO RETRY";
                    break;

                case GameState.TimeUp:
                    // リザルト画面（T13）ができるまでは、Console と仮の表示に出すだけ
                    Debug.Log($"TIME UP（仮）: 正解数 {correctCount}／スコア {score}", this);
                    txtMessege.text = "TIME UP";
                    txtReturn.text = "ENTER TO RETRY";
                    break;

                default:
                    // ほかのステートの処理は、それぞれのタスク（T11・T16）で足す
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
            // ローカル変数は関数の先頭で宣言する
            QuestionData question;   // 1問分の出題内容

            // 今の正解数で解禁されているスタイルだけから選ぶ（正解を重ねるほど出てくる書体が増える）
            question = questionGenerator.Generate(correctCount);
            currentAnswer = question.Answer;
            currentStyleName = question.Style.name;
            glyphView.Show(question);
            questionElapsedSeconds = 0f;
        }

        /// <summary>
        /// Resources のフォルダから出題スタイルをすべて読み込む（フォルダに置くだけで出題に混ざる。SPEC §1.4）。
        /// 書体がないスタイル、出やすさが 0 のスタイル、書体に英字側か数字側の文字がないスタイルは使わない。
        /// 1問目から出せるスタイル（解禁する正解数が 0）が1つもないと1問目を出せないので、そのときも止める
        /// </summary>
        /// <returns>使えるスタイルが1つ以上あり、そのうち1問目から出せるものもあれば true</returns>
        private bool LoadStyles()
        {
            // ローカル変数は関数の先頭で宣言する
            GlyphStyleData[] foundStyles;   // フォルダから見つかったスタイル
            int startStyleCount;            // 1問目から出せるスタイルの数（解禁する正解数が 0 のもの）
            int i;                          // ループ用の添字

            foundStyles = Resources.LoadAll<GlyphStyleData>(styleResourcesFolder);
            loadedStyles.Clear();
            startStyleCount = 0;
            for (i = 0; i < foundStyles.Length; i++)
            {
                // 書体がないと表示できないので外す（Inspector でも警告が出ている）
                if (foundStyles[i].FontAsset == null)
                {
                    Debug.LogWarning($"GlyphStyle「{foundStyles[i].name}」は書体が未設定なので、出題に使いません", foundStyles[i]);
                    continue;
                }

                // 出やすさが 0 のスタイルは、わざと出さない設定なので外す
                if (foundStyles[i].Weight <= 0f)
                {
                    continue;
                }

                // 書体に英字側か数字側の文字がないと、その文字だけ別の書体で表示されて答えの手がかりになるので外す
                if (!foundStyles[i].HasBothGlyphs())
                {
                    Debug.LogWarning($"GlyphStyle「{foundStyles[i].name}」は書体に英字側か数字側の文字が入っていないので、出題に使いません（書体を作り直してください）", foundStyles[i]);
                    continue;
                }

                loadedStyles.Add(foundStyles[i]);

                // 1問目から出せるスタイルを数える（正解数 0 で解禁されているもの）
                if (foundStyles[i].UnlockCorrectCount <= 0)
                {
                    startStyleCount++;
                }
            }

            if (loadedStyles.Count == 0)
            {
                Debug.LogError($"GameFlowController: 出題に使えるスタイルが1つもありません（Assets/OrZero/Resources/{styleResourcesFolder}/ に、書体を設定した Glyph Style を置いてください）", this);
                return false;
            }

            if (startStyleCount == 0)
            {
                Debug.LogError("GameFlowController: 1問目から出せるスタイルがありません（Glyph Style の Unlock Correct Count（解禁する正解数）を 0 にしたものを、1つ以上置いてください）", this);
                return false;
            }

            return true;
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

            if (balanceData == null || glyphView == null || answerButtons == null || hudView == null
                || txtMessege == null || txtReturn == null)
            {
                Debug.LogError("GameFlowController: balanceData・glyphView・answerButtons・hudView・txtMessege・txtReturn を Inspector で設定してください", this);
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
