// 引入必要的程序包（类似工具箱）
using System.Collections;        // 基础系统工具（虽然本例未直接使用，但保留无害）
using System.Collections.Generic; // 列表等数据结构工具（本例未直接使用）
using UnityEngine;               // Unity引擎核心功能
using Unity.MLAgents;           // ML-Agents机器学习框架核心
using Unity.MLAgents.Sensors;   // 用于Agent的观察功能
using Unity.MLAgents.Actuators; // 新增的命名空间

// 定义RollerAgent类，继承自Agent基类（冒号表示继承）
public class RollerAgent : Agent // 类名必须与文件名一致
{
    // 【字段声明】
    // Rigidbody类型变量：用于物理模拟的刚体组件
    // private表示仅本类可以访问（未写修饰符时默认为private）
    Rigidbody rBody;

    // public表示可以在Unity编辑器中设置，Transform类型存储物体的位置/旋转/缩放信息
    public Transform Target;     // 目标物体的位置信息

    // float表示浮点数（带小数），public变量会显示在Unity Inspector面板
    public float forceMultiplier = 10; // 控制移动力度的放大系数

    // Start方法：Unity的初始化函数，在对象创建后第一帧更新前调用
    void Start()
    {
        // GetComponent<类型>() 获取当前物体上的指定类型组件
        // 这里获取Rigidbody组件并赋值给rBody变量
        rBody = GetComponent<Rigidbody>();
    }

    // OnEpisodeBegin方法：ML-Agents的重写方法，每个训练回合开始时调用
    public override void OnEpisodeBegin()
    {
        // 判断代理的Y轴位置是否小于0（掉下平台）
        // transform是当前物体的Transform组件，localPosition是局部坐标位置
        if (this.transform.localPosition.y < 0) // this可省略，表示当前实例
        {
            // 重置物理状态
            rBody.angularVelocity = Vector3.zero; // 角速度归零
            rBody.velocity = Vector3.zero;        // 线性速度归零
            // 重置位置到平台中心（0,0.5,0），Y=0.5因为球体半径是0.5
            this.transform.localPosition = new Vector3(0, 0.5f, 0);
        }

        // 设置目标物体的新位置：
        // Random.value返回0-1的随机数，8-4的操作使范围变为-4到+4
        // 保持Y轴0.5（立方体高度为1，放在平台表面）
        Target.localPosition = new Vector3(
            Random.value * 8 - 4, // X轴：-4到4
            0.5f,                 // Y轴固定
            Random.value * 8 - 4  // Z轴：-4到4
        );
    }

    // CollectObservations方法：收集环境观察数据供AI学习
    public override void CollectObservations(VectorSensor sensor)
    {
        // 添加目标位置观察（3个浮点数：x,y,z）
        sensor.AddObservation(Target.localPosition); // 加入观察列表

        // 添加代理自身位置观察（3个浮点数）
        sensor.AddObservation(this.transform.localPosition);

        // 添加速度观察（只需要x和z轴，y轴速度不影响平面移动）
        sensor.AddObservation(rBody.velocity.x); // X轴速度
        sensor.AddObservation(rBody.velocity.z); // Z轴速度
        // 总观察值数量 = 3 + 3 + 2 = 8
    }

    // OnActionReceived方法：处理AI决策的动作输入
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // 从动作缓冲区获取连续动作（AI的输出）
        // ActionBuffers包含离散和连续动作，这里用连续动作[0]和[1]
        Vector3 controlSignal = Vector3.zero; // 初始化三维向量（0,0,0）
        controlSignal.x = actionBuffers.ContinuousActions[0]; // 第一个动作控制X轴
        controlSignal.z = actionBuffers.ContinuousActions[1]; // 第二个动作控制Z轴

        // 应用力到刚体（ForceMode默认为Force，持续施加力）
        rBody.AddForce(controlSignal * forceMultiplier);

        // 计算与目标的距离（使用三维空间距离公式）
        float distanceToTarget = Vector3.Distance(
            this.transform.localPosition,
            Target.localPosition
        );

        // 判断是否到达目标（1.42是经验值，因为立方体对角线≈1.414）
        if (distanceToTarget < 1.42f)
        {
            SetReward(1.0f);  // 给予1分奖励
            EndEpisode();     // 结束当前回合
        }
        // 如果掉下平台（Y坐标小于0）
        else if (this.transform.localPosition.y < 0)
        {
            EndEpisode(); // 结束回合（不给奖励）
        }
    }

    // 在训练期间或调试时，通过键盘输入手动控制代理的行为
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }
}

