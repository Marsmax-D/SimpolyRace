using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public CarController target; // 相机要跟随的目标赛车
    private Vector3 offsetDir; // 相机相对于目标赛车的偏移方向向量

    public float minDistance, maxDistance; // 相机距离目标赛车的最小和最大距离
    private float activeDistance; // 当前有效距离，根据赛车速度动态调整

    public Transform startTargetOffset; // 初始时相机位置的参考偏移点

    // Start is called before the first frame update
    void Start()
    {
        // 计算相机相对于目标赛车的偏移方向向量
        offsetDir = transform.position - startTargetOffset.position;

        // 初始时设置相机距离为最小距离
        activeDistance = minDistance;

        // 将偏移方向向量标准化（长度为1），用于计算相机位置
        offsetDir.Normalize();
    }

    // Update is called once per frame
    void Update()
    {
        // 根据赛车速度动态调整相机距离
        // 计算当前有效距离，根据赛车速度在最小距离和最大距离之间进行插值
        activeDistance = minDistance + ((maxDistance - minDistance) * (target.theRB.velocity.magnitude / target.maxSpeed));

        // 设置相机位置为目标赛车位置加上偏移方向向量乘以当前有效距离
        transform.position = target.transform.position + (offsetDir * activeDistance);
    }
}
