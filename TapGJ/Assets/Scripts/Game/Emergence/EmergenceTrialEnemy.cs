using System.Reflection;
using UnityEngine;

/// <summary>Small scene-local trial modifiers, independent from permanent gene upgrades.</summary>
public sealed class EmergenceTrialEnemy:MonoBehaviour
{
    public void Configure(int variation)
    {
        EnemyAIController enemy=GetComponent<EnemyAIController>();
        if(enemy==null)return;
        if(variation==2)
        {
            FieldInfo field=typeof(EnemyAIController).GetField("chaseSpeed",BindingFlags.Instance|BindingFlags.NonPublic);
            if(field!=null)field.SetValue(enemy,(float)field.GetValue(enemy)*1.2f);
        }
    }
}
