using UnityEngine;

namespace TapGJ.WaterProjectiles
{
    /// <summary>Liquid presentation on a child transform, independent of the shot's collision and trajectory.</summary>
    public sealed class WaterProjectileVisual : MonoBehaviour
    {
        private Transform surface;
        private Vector2 lastPosition, heading;
        private float motionSpeed, born, phase;
        private bool configured;
        public SpriteRenderer Surface { get; private set; }

        public void Configure(Vector2 direction, float speed)
        {
            heading=direction.sqrMagnitude>.0001f?direction.normalized:Vector2.right;
            motionSpeed=speed;
            lastPosition=transform.position;
            born=Time.time;
            if(!configured)
            {
                configured=true;
                SpriteRenderer original=GetComponent<SpriteRenderer>();
                int order=original!=null?original.sortingOrder:12;
                int layer=original!=null?original.sortingLayerID:0;
                if(original!=null) original.enabled=false;
                var child=new GameObject("LiquidSurface");
                child.layer=gameObject.layer;
                surface=child.transform; surface.SetParent(transform,false);
                Surface=child.AddComponent<SpriteRenderer>();
                Surface.sprite=WaterVfx.GetLiquidQuad();
                Surface.sharedMaterial=WaterVfx.GetLiquidMaterial(0);
                Surface.sortingLayerID=layer; Surface.sortingOrder=Mathf.Max(12,order);
                phase=Random.value*Mathf.PI*2;
                var properties=new MaterialPropertyBlock(); properties.SetFloat("_Phase",phase);
                Surface.SetPropertyBlock(properties);
                float radius=Mathf.Max(.025f,transform.lossyScale.x*.5f);
                WaterVfx.AddLiquidRibbon(transform,radius,Surface.sortingOrder-1,layer);
                WaterVfx.AddLiquidDroplets(transform,radius,Surface.sortingOrder-1,layer);
            }
            Orient();
        }

        private void LateUpdate()
        {
            if(!configured||Time.deltaTime<=0) return;
            Vector2 position=transform.position;
            Vector2 displacement=position-lastPosition;
            if(displacement.sqrMagnitude>.000001f)
            { heading=displacement.normalized; motionSpeed=displacement.magnitude/Time.deltaTime; }
            lastPosition=position;
            Orient();
        }

        private void Orient()
        {
            Vector3 localHeading=transform.InverseTransformDirection(heading);
            surface.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(localHeading.y,localHeading.x)*Mathf.Rad2Deg);
            float flow=Mathf.Clamp01(motionSpeed/12f);
            float wobble=Mathf.Sin((Time.time-born)*18+phase)*.045f;
            // Stretch and recoil only the visible body; the collider stays circular and unchanged.
            surface.localScale=new Vector3(1.12f+flow*.34f+wobble,.98f-flow*.20f-wobble,1);
        }
    }
}
