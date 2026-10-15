using UnityEngine;

namespace TapGJ.WaterProjectiles
{
    public sealed class WaterImpactSheet : MonoBehaviour
    {
        private MaterialPropertyBlock properties;
        private SpriteRenderer surface;
        private float age;
        private void Awake() { surface=GetComponent<SpriteRenderer>(); properties=new MaterialPropertyBlock(); }
        private void Update()
        {
            age+=Time.deltaTime;
            properties.SetFloat("_Progress",Mathf.Clamp01(age/.32f));
            surface.SetPropertyBlock(properties);
            if(age>=.32f) Destroy(gameObject);
        }
    }
}
