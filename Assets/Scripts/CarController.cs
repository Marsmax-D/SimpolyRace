using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public Rigidbody theRB; // 车辆的刚体组件

    public float maxSpeed; // 车辆的最大速度

    // 加速度和减速度参数
    public float forwardAccel = 8f, reverseAccel = 4f;
    private float speedInput; // 车辆速度输入

    public float turnStrength = 180f; // 车辆转向强度
    private float turnInput; // 车辆转向输入

    private bool grounded; // 车辆是否接触地面

    public Transform groundRayPoint, groundRayPoint2; // 地面检测射线起点
    public LayerMask whatIsGround; // 地面的图层
    public float groundRayLength = .75f; // 地面检测射线长度

    private float dragOnGround; // 接地时的阻力
    public float gravityMod = 10f; // 重力调节参数

    public Transform leftFrontWheel, rightFrontWheel; // 前轮的位置
    public float maxWheelTurn = 25f; // 前轮最大转向角度

    public ParticleSystem[] dustTrail; // 车辆尾部烟尘效果
    public float maxEmission = 25f, emissionFadeSpeed = 20f; // 最大排放量和排放量渐变速度
    private float emissionRate; // 当前排放量

    public AudioSource engineSound, skidSound; // 引擎声音和刹车声音
    public float skidFadeSpeed; // 刹车声音渐变速度

    public int nextCheckpoint; // 下一个检查点编号
    public int currentLap; // 当前圈数

    public float lapTime, bestLapTime; // 当前圈的时间和最佳圈时间

    public float resetCooldown = 2f; // 重置冷却时间
    private float resetCounter; // 重置计时器

    public bool isAI; // 是否为 AI 控制

    public int currentTarget; // 当前目标检查点索引
    private Vector3 targetPoint; // 当前目标点
    public float aiAccelerateSpeed = 1f, aiTurnSpeed = .8f, aiReachPointRange = 5f, aiPointVariance = 3f, aiMaxTurn = 15f;
    private float aiSpeedInput, aiSpeedMod; // AI 控制的速度输入和速度模式

    // Start is called before the first frame update
    void Start()
    {
        theRB.transform.parent = null; // 将刚体的父对象设置为空

        dragOnGround = theRB.drag; // 记录接地时的阻力

        // 如果是 AI 控制，设置初始目标点和速度模式
        if (isAI)
        {
            targetPoint = RaceManager.instance.allCheckpoints[currentTarget].transform.position;
            RandomiseAITarget();

            aiSpeedMod = Random.Range(.8f, 1.1f); // 随机设置 AI 速度模式
        }

        // 更新 UI 显示当前圈数
        UIManager.instance.lapCounterText.text = currentLap + "/" + RaceManager.instance.totalLaps;

        resetCounter = resetCooldown; // 初始化重置计时器
    }

    // Update is called once per frame
    void Update()
    {
        // 如果比赛未开始，更新圈数计时
        if (!RaceManager.instance.isStarting)
        {
            lapTime += Time.deltaTime;

            // 如果不是 AI 控制，更新当前圈的时间显示
            if (!isAI)
            {
                var ts = System.TimeSpan.FromSeconds(lapTime);
                UIManager.instance.currentLapTimeText.text = string.Format("{0:00}m{1:00}.{2:000}s", ts.Minutes, ts.Seconds, ts.Milliseconds);

                // 处理玩家输入
                speedInput = 0f;
                if (Input.GetAxis("Vertical") > 0)
                {
                    speedInput = Input.GetAxis("Vertical") * forwardAccel;
                }
                else if (Input.GetAxis("Vertical") < 0)
                {
                    speedInput = Input.GetAxis("Vertical") * reverseAccel;
                }

                turnInput = Input.GetAxis("Horizontal");

                // 处理重置车辆位置的输入
                if (resetCounter > 0)
                {
                    resetCounter -= Time.deltaTime;
                }

                if (Input.GetKeyDown(KeyCode.R) && resetCounter <= 0)
                {
                    ResetToTrack();
                }
            }
            else
            {
                targetPoint.y = transform.position.y;

                // 当车辆接近目标点时，设置下一个目标点
                if (Vector3.Distance(transform.position, targetPoint) < aiReachPointRange)
                {
                    SetNextAITarget();
                }

                // 计算目标点与车辆之间的角度
                Vector3 targetDir = targetPoint - transform.position;
                float angle = Vector3.Angle(targetDir, transform.forward);

                // 计算转向输入
                Vector3 localPos = transform.InverseTransformPoint(targetPoint);
                if (localPos.x < 0f)
                {
                    angle = -angle;
                }

                turnInput = Mathf.Clamp(angle / aiMaxTurn, -1f, 1f);

                // 根据角度调整速度输入
                if (Mathf.Abs(angle) < aiMaxTurn)
                {
                    aiSpeedInput = Mathf.MoveTowards(aiSpeedInput, 1f, aiAccelerateSpeed);
                }
                else
                {
                    aiSpeedInput = Mathf.MoveTowards(aiSpeedInput, aiTurnSpeed, aiAccelerateSpeed);
                }

                speedInput = aiSpeedInput * forwardAccel * aiSpeedMod;
            }

            // 控制前轮转向角度
            leftFrontWheel.localRotation = Quaternion.Euler(leftFrontWheel.localRotation.eulerAngles.x, (turnInput * maxWheelTurn) - 180, leftFrontWheel.localRotation.eulerAngles.z);
            rightFrontWheel.localRotation = Quaternion.Euler(rightFrontWheel.localRotation.eulerAngles.x, (turnInput * maxWheelTurn), rightFrontWheel.localRotation.eulerAngles.z);

            // 控制尾部烟尘效果的排放
            emissionRate = Mathf.MoveTowards(emissionRate, 0f, emissionFadeSpeed * Time.deltaTime);

            if (grounded && (Mathf.Abs(turnInput) > .5f || (theRB.velocity.magnitude < maxSpeed * .5f && theRB.velocity.magnitude != 0)))
            {
                emissionRate = maxEmission;
            }

            if (theRB.velocity.magnitude <= .5f)
            {
                emissionRate = 0;
            }

            for (int i = 0; i < dustTrail.Length; i++)
            {
                var emissionModule = dustTrail[i].emission;
                emissionModule.rateOverTime = emissionRate;
            }

            // 更新引擎声音的音调
            if (engineSound != null)
            {
                engineSound.pitch = 1f + ((theRB.velocity.magnitude / maxSpeed) * 2f);
            }

            // 更新刹车声音的音量
            if (skidSound != null)
            {
                if (Mathf.Abs(turnInput) > 0.5f)
                {
                    skidSound.volume = 1f;
                }
                else
                {
                    skidSound.volume = Mathf.MoveTowards(skidSound.volume, 0f, skidFadeSpeed * Time.deltaTime);
                }
            }
        }
    }

    // 在 FixedUpdate 中处理物理相关的计算
    private void FixedUpdate()
    {
        grounded = false;

        RaycastHit hit;
        Vector3 normalTarget = Vector3.zero;

        // 射线检测车辆是否接触地面
        if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLength, whatIsGround))
        {
            grounded = true;
            normalTarget = hit.normal;
        }

        if (Physics.Raycast(groundRayPoint2.position, -transform.up, out hit, groundRayLength, whatIsGround))
        {
            grounded = true;
            normalTarget = (normalTarget + hit.normal) / 2f;
        }

        // 当车辆接触地面时，调整车辆的旋转以匹配地面法线
        if (grounded)
        {
            transform.rotation = Quaternion.FromToRotation(transform.up, normalTarget) * transform.rotation;
        }

        // 根据车辆是否接触地面应用不同的力和阻力
        if (grounded)
        {
            theRB.drag = dragOnGround;
            theRB.AddForce(transform.forward * speedInput * 1000f);
        }
        else
        {
            theRB.drag = .1f;
            theRB.AddForce(-Vector3.up * gravityMod * 100f);
        }

        // 限制车辆速度
        if (theRB.velocity.magnitude > maxSpeed)
        {
            theRB.velocity = theRB.velocity.normalized * maxSpeed;
        }

        transform.position = theRB.position;

        // 根据转向输入调整车辆的旋转角度
        if (grounded && speedInput != 0)
        {
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0f, turnInput * turnStrength * Time.deltaTime * Mathf.Sign(speedInput) * (theRB.velocity.magnitude / maxSpeed), 0f));
        }
    }

    // 检测到检查点触发时调用的方法
    public void CheckpointHit(int cpNumber)
    {
        if (cpNumber == nextCheckpoint)
        {
            nextCheckpoint++;

            // 如果通过了所有检查点，完成一圈
            if (nextCheckpoint == RaceManager.instance.allCheckpoints.Length)
            {
                nextCheckpoint = 0;
                LapCompleted();
            }
        }

        // 如果是 AI 控制，检测是否达到当前目标检查点
        if (isAI)
        {
            if (cpNumber == currentTarget)
            {
                SetNextAITarget();
            }
        }
    }

    // 设置下一个 AI 控制的目标点
    public void SetNextAITarget()
    {
        currentTarget++;
        if (currentTarget >= RaceManager.instance.allCheckpoints.Length)
        {
            currentTarget = 0;
        }

        targetPoint = RaceManager.instance.allCheckpoints[currentTarget].transform.position;
        RandomiseAITarget();
    }

    // 完成一圈时调用的方法
    public void LapCompleted()
    {
        currentLap++;

        // 更新最佳圈时间
        if (lapTime < bestLapTime || bestLapTime == 0)
        {
            bestLapTime = lapTime;
        }

        // 如果未完成所有圈数，重置圈数计时并更新 UI
        if (currentLap <= RaceManager.instance.totalLaps)
        {
            lapTime = 0f;

            if (!isAI)
            {
                var ts = System.TimeSpan.FromSeconds(bestLapTime);
                UIManager.instance.bestLapTimeText.text = string.Format("{0:00}m{1:00}.{2:000}s", ts.Minutes, ts.Seconds, ts.Milliseconds);

                UIManager.instance.lapCounterText.text = currentLap + "/" + RaceManager.instance.totalLaps;
            }
        }
        else
        {
            // 如果是玩家车辆，切换到 AI 控制并结束比赛
            if (!isAI)
            {
                isAI = true;
                aiSpeedMod = 1f;

                targetPoint = RaceManager.instance.allCheckpoints[currentTarget].transform.position;
                RandomiseAITarget();

                var ts = System.TimeSpan.FromSeconds(bestLapTime);
                UIManager.instance.bestLapTimeText.text = string.Format("{0:00}m{1:00}.{2:000}s", ts.Minutes, ts.Seconds, ts.Milliseconds);

                RaceManager.instance.FinishRace();
            }
        }
    }

    // 随机调整 AI 控制的目标点位置
    public void RandomiseAITarget()
    {
        targetPoint += new Vector3(Random.Range(-aiPointVariance, aiPointVariance), 0f, Random.Range(-aiPointVariance, aiPointVariance));
    }

    // 将车辆重置到轨道上
    void ResetToTrack()
    {
        int pointToGoTo = nextCheckpoint - 1;
        if (pointToGoTo < 0)
        {
            pointToGoTo = RaceManager.instance.allCheckpoints.Length - 1;
        }

        transform.position = RaceManager.instance.allCheckpoints[pointToGoTo].transform.position;
        theRB.transform.position = transform.position;
        theRB.velocity = Vector3.zero;

        speedInput = 0f;
        turnInput = 0f;

        resetCounter = resetCooldown;
    }

    // 用于录制视频，切换到 AI 控制
    public void SwitchToAI()
    {
        aiSpeedMod = 1f;
        targetPoint = RaceManager.instance.allCheckpoints[currentTarget].transform.position;
        RandomiseAITarget();
    }
}
