using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    // 创建一个静态的 UIManager 实例，以便在整个游戏中访问
    public static UIManager instance;

    // UI 元素的引用
    public TMP_Text lapCounterText;      // 显示圈数的文本
    public TMP_Text bestLapTimeText;     // 显示最佳圈时间的文本
    public TMP_Text currentLapTimeText;  // 显示当前圈时间的文本
    public TMP_Text positionText;        // 显示车辆位置的文本
    public TMP_Text countDownText;       // 显示倒计时文本
    public TMP_Text goText;              // 显示出发提示文本
    public TMP_Text raceResultText;      // 显示比赛结果的文本

    public GameObject resultsScreen;     // 比赛结果界面
    public GameObject pauseScreen;       // 暂停界面
    public GameObject trackUnlockedMessage; // 路径解锁提示

    public bool isPaused;  // 游戏是否暂停的标志

    private void Awake()
    {
        // 在 Awake 方法中初始化 UIManager 实例，确保整个游戏中只有一个实例
        instance = this;
    }

    // Start 方法在脚本启用时调用，这里留空
    void Start()
    {

    }

    // Update 方法在每帧更新时调用，用于检测输入是否按下 Escape 键
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 按下 Escape 键时执行暂停或继续游戏的方法
            PauseUnpause();
        }
    }

    // 暂停或继续游戏的方法
    public void PauseUnpause()
    {
        // 切换暂停状态
        isPaused = !isPaused;

        // 根据暂停状态设置暂停界面的显示和隐藏
        pauseScreen.SetActive(isPaused);

        // 根据暂停状态设置游戏时间缩放
        if (isPaused)
        {
            Time.timeScale = 0f; // 暂停游戏
        }
        else
        {
            Time.timeScale = 1f; // 恢复游戏
        }
    }

    // 退出比赛的方法
    public void ExitRace()
    {
        Time.timeScale = 1f; // 恢复游戏时间缩放
        RaceManager.instance.ExitRace(); // 调用 RaceManager 中的退出比赛方法
    }

    // 退出游戏的方法
    public void QuitGame()
    {
        Application.Quit(); // 退出应用程序
        Debug.Log("Game Quit"); // 输出日志，用于调试
    }
}
