namespace TapGJ.WaterProjectiles
{
    /// <summary>在敌人已有的血量组件上实现此接口，并在方法中调用其扣血逻辑。</summary>
    public interface IWaterDamageable
    {
        void TakeWaterDamage(float damage);
    }
}
