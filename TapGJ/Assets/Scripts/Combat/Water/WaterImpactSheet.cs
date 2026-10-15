using UnityEngine;

namespace TapGJ.WaterProjectiles
{
    public sealed class WaterImpactSheet : MonoBehaviour
    {
        private MaterialPropertyBlock properties;
        private SpriteRenderer surface;
        private float age;
        private void Awake() { surface=GetComponent<SpriteRenderer>(); properties=new MaterialPropertyBlock(); }
        private void OnEnable() { age=0; if(surface!=null){properties.Clear();properties.SetFloat("_Progress",0);surface.SetPropertyBlock(properties);} }
        private void Update()
        {
            age+=Time.deltaTime;
            properties.SetFloat("_Progress",Mathf.Clamp01(age/.32f));
            surface.SetPropertyBlock(properties);
            // The scene-local impact pool returns this object after the shader animation finishes.
        }
    }
}
