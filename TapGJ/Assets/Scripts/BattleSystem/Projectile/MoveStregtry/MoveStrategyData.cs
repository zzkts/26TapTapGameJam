using System;
using UnityEngine;

/// <summary>
/// 所有子弹移动策略的参数配置。
/// 每种策略只在自己的配置块中保存需要的参数。
/// </summary>
[CreateAssetMenu(menuName = "Battle/Move Strategy Data", fileName = "MoveStrategyData")]
public class MoveStrategyData : ScriptableObject
{
    [Serializable]
    public struct OrbitData
    {
        [Min(0.1f)] public float radius;
        [Min(0f)] public float angularSpeed;
    }

    public OrbitData orbit;
}
