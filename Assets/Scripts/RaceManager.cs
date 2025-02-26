using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RaceManager : MonoBehaviour
{
    public static RaceManager instance;

    public Checkpoint[] allCheckpoints; // 所有赛道检查点的数组
    public int totalLaps; // 总圈数

    public CarController playerCar; // 玩家赛车
    public List<CarController> allAICars = new List<CarController>(); // 所有AI赛车列表
    public int playerPosition; // 玩家赛车的位置
    public float timeBetweenPosCheck = .2f; // 每次检查赛车位置的时间间隔
    private float posChkCounter; // 赛车位置检查计时器

    public float aiDefaultSpeed = 30f, playerDefaultSpeed = 30f; // AI赛车和玩家赛车的默认速度
    public float rubberBandSpeedMod = 3.5f, rubBandAccel = .5f; // 橡皮筋效果的速度调整参数

    public bool isStarting; // 是否比赛开始中
    public float timeBetweenStartCount = 1f; // 比赛开始倒计时间隔
    private float startCounter; // 比赛开始倒计时计时器
    public int countdownCurrent = 3; // 倒计时当前数字

    public int playerStartPosition, aiNumberToSpawn; // 玩家赛车起始位置和要生成的AI赛车数量
    public Transform[] startPoints; // 起始位置数组
    public List<CarController> carsToSpawn = new List<CarController>(); // 待生成的赛车列表

    public bool raceCompleted; // 是否比赛完成
    public string raceCompleteScene; // 比赛结束后要加载的场景名称

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        // 从RaceInfoManager中获取比赛设定参数
        totalLaps = RaceInfoManager.instance.noOfLaps;
        aiNumberToSpawn = RaceInfoManager.instance.noOfAI;

        // 初始化所有赛道检查点的编号
        for (int i = 0; i < allCheckpoints.Length; i++)
        {
            allCheckpoints[i].cpNumber = i;
        }

        // 设置比赛开始状态，进行倒计时
        isStarting = true;
        startCounter = timeBetweenStartCount;

        // 更新UI显示倒计时数字
        UIManager.instance.countDownText.text = countdownCurrent + "!";

        // 随机选择玩家赛车的起始位置
        playerStartPosition = Random.Range(0, aiNumberToSpawn + 1);

        // 在起始位置生成玩家赛车，并设置为非AI控制
        playerCar = Instantiate(RaceInfoManager.instance.racerToUse, startPoints[playerStartPosition].position, startPoints[playerStartPosition].rotation);
        playerCar.isAI = false;
        playerCar.GetComponent<AudioListener>().enabled = true;

        // 设置摄像机跟随玩家赛车
        CameraSwitcher.instance.SeTarget(playerCar);

        // 生成AI赛车
        for (int i = 0; i < aiNumberToSpawn + 1; i++)
        {
            if (i != playerStartPosition)
            {
                // 随机选择要生成的AI赛车
                int selectedCar = Random.Range(0, carsToSpawn.Count);

                // 在指定位置生成AI赛车
                allAICars.Add(Instantiate(carsToSpawn[selectedCar], startPoints[i].position, startPoints[i].rotation));

                // 生成后移除已使用的赛车
                if (carsToSpawn.Count > aiNumberToSpawn - i)
                {
                    carsToSpawn.RemoveAt(selectedCar);
                }
            }
        }

        // 更新UI显示玩家赛车的起始位置
        UIManager.instance.positionText.text = (playerStartPosition + 1) + "/" + (allAICars.Count + 1);
    }

    // Update is called once per frame
    void Update()
    {
        if (isStarting)
        {
            // 进行比赛开始倒计时
            startCounter -= Time.deltaTime;
            if (startCounter <= 0)
            {
                countdownCurrent--;
                startCounter = timeBetweenStartCount;

                // 更新UI显示倒计时数字
                UIManager.instance.countDownText.text = countdownCurrent + "!";

                if (countdownCurrent == 0)
                {
                    // 倒计时结束，开始比赛，隐藏倒计时文本，显示开始文本
                    isStarting = false;
                    UIManager.instance.countDownText.gameObject.SetActive(false);
                    UIManager.instance.goText.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            // 更新赛车位置和排名
            posChkCounter -= Time.deltaTime;
            if (posChkCounter <= 0)
            {
                // 重新计算赛车位置和排名
                CalculatePlayerPosition();
                posChkCounter = timeBetweenPosCheck;

                // 更新UI显示玩家排名
                UIManager.instance.positionText.text = playerPosition + "/" + (allAICars.Count + 1);
            }

            // 管理橡皮筋效果
            ManageRubberBandEffect();
        }

        // 测试功能，按键控制UI显示和玩家赛车控制模式切换
        HandleTestingInputs();
    }

    // 通过圈数和检查点来计算玩家赛车的位置和排名
    private void CalculatePlayerPosition()
    {
        playerPosition = 1;

        foreach (CarController aiCar in allAICars)
        {
            if (aiCar.currentLap > playerCar.currentLap)
            {
                playerPosition++;
            }
            else if (aiCar.currentLap == playerCar.currentLap)
            {
                if (aiCar.nextCheckpoint > playerCar.nextCheckpoint)
                {
                    playerPosition++;
                }
                else if (aiCar.nextCheckpoint == playerCar.nextCheckpoint)
                {
                    if (Vector3.Distance(aiCar.transform.position, allCheckpoints[aiCar.nextCheckpoint].transform.position) < Vector3.Distance(playerCar.transform.position, allCheckpoints[aiCar.nextCheckpoint].transform.position))
                    {
                        playerPosition++;
                    }
                }
            }
        }
    }

    // 管理橡皮筋效果
    private void ManageRubberBandEffect()
    {
        if (playerPosition == 1)
        {
            // 玩家排名第一，增加AI赛车的速度，降低玩家赛车的速度
            foreach (CarController aiCar in allAICars)
            {
                aiCar.maxSpeed = Mathf.MoveTowards(aiCar.maxSpeed, aiDefaultSpeed + rubberBandSpeedMod, rubBandAccel * Time.deltaTime);
            }

            playerCar.maxSpeed = Mathf.MoveTowards(playerCar.maxSpeed, playerDefaultSpeed - rubberBandSpeedMod, rubBandAccel * Time.deltaTime);
        }
        else
        {
            // 根据玩家排名调整AI赛车和玩家赛车的速度
            foreach (CarController aiCar in allAICars)
            {
                float speedMod = rubberBandSpeedMod * ((float)playerPosition / ((float)allAICars.Count + 1));
                aiCar.maxSpeed = Mathf.MoveTowards(aiCar.maxSpeed, aiDefaultSpeed - speedMod, rubBandAccel * Time.deltaTime);
            }

            float playerSpeedMod = rubberBandSpeedMod * ((float)playerPosition / ((float)allAICars.Count + 1));
            playerCar.maxSpeed = Mathf.MoveTowards(playerCar.maxSpeed, playerDefaultSpeed + playerSpeedMod, rubBandAccel * Time.deltaTime);
        }
    }

    // 处理测试用按键输入
    private void HandleTestingInputs()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            // 按下H键切换UI显示状态
            UIManager.instance.gameObject.SetActive(!UIManager.instance.gameObject.activeInHierarchy);
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            // 按下J键切换玩家赛车控制模式（人类控制或AI控制）
            playerCar.isAI = !playerCar.isAI;
            playerCar.SwitchToAI();
        }
    }

    // 比赛结束方法
    public void FinishRace()
    {
        raceCompleted = true;

        switch (playerPosition)
        {
            case 1:
                // 如果玩家排名第一，显示相应的比赛结果和解锁新赛道消息
                UIManager.instance.raceResultText.text = "你排名第 1 !!";
                if (RaceInfoManager.instance.trackToUnlock != "")
                {
                    if (!PlayerPrefs.HasKey(RaceInfoManager.instance.trackToUnlock + "_unlocked"))
                    {
                        PlayerPrefs.SetInt(RaceInfoManager.instance.trackToUnlock + "_unlocked", 1);
                        UIManager.instance.trackUnlockedMessage.SetActive(true);
                    }
                }
                break;

            case 2:
                // 如果玩家排名第二，显示相应的比赛结果
                UIManager.instance.raceResultText.text = "你排名第 2 !!";
                break;

            case 3:
                // 如果玩家排名第三，显示相应的比赛结果
                UIManager.instance.raceResultText.text = "你排名第 3 !!";
                break;

            default:
                // 其他排名情况，显示相应的比赛结果
                UIManager.instance.raceResultText.text = "你排名第 " + playerPosition + "！！";
                break;
        }

        // 显示比赛结果界面
        UIManager.instance.resultsScreen.SetActive(true);
    }

    // 退出比赛方法
    public void ExitRace()
    {
        // 加载指定的比赛结束场景
        SceneManager.LoadScene(raceCompleteScene);
    }
}
