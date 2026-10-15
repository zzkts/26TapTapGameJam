using UnityEngine;

namespace TapGJ.WaterProjectiles
{
    /// <summary>Procedural Shuriken particles confined to the game's XY plane.</summary>
    public static class WaterVfx
    {
        private static Sprite dropletSprite;
        private static Material particleMaterial;
        private static Material spriteMaterial;
        private static Sprite liquidQuad;
        private static readonly Material[] liquidMaterials=new Material[4];

        public static Sprite GetLiquidQuad()
        {
            if(liquidQuad==null)
            {
                Texture2D texture=Texture2D.whiteTexture;
                liquidQuad=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f,
                    texture.width,0,SpriteMeshType.FullRect);
                liquidQuad.name="Liquid shader quad";
            }
            return liquidQuad;
        }

        public static Material GetLiquidMaterial(int kind)
        {
            if(liquidMaterials[kind]==null)
                liquidMaterials[kind]=Resources.Load<Material>("WaterProjectiles/"+new[]{"LiquidDrop","LiquidRibbon","LiquidSpray","LiquidImpact"}[kind]);
            return liquidMaterials[kind];
        }

        public static void AddLiquidRibbon(Transform parent,float radius,int order,int layer)
        {
            var obj=new GameObject("LiquidRibbon"); obj.layer=parent.gameObject.layer;
            obj.transform.SetParent(parent,false);
            TrailRenderer trail=obj.AddComponent<TrailRenderer>();
            trail.sharedMaterial=GetLiquidMaterial(1);
            trail.time=.11f; trail.minVertexDistance=.015f;
            trail.widthCurve=new AnimationCurve(new Keyframe(0,1),new Keyframe(.28f,.55f),new Keyframe(1,0));
            trail.widthMultiplier=radius*.8f;
            trail.numCapVertices=4; trail.numCornerVertices=4;
            trail.textureMode=LineTextureMode.Stretch;
            trail.sortingOrder=order; trail.sortingLayerID=layer;
            trail.startColor=new Color(1,1,1,.75f); trail.endColor=new Color(1,1,1,0);
        }

        public static void AddLiquidDroplets(Transform parent,float radius,int order,int layer)
        {
            ParticleSystem particles=CreateParticles("LiquidDroplets",parent,Color.white,order);
            particles.gameObject.layer=parent.gameObject.layer;
            var renderer=particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial=GetLiquidMaterial(2); renderer.sortingLayerID=layer;
            var main=particles.main; main.loop=true;
            main.startLifetime=new ParticleSystem.MinMaxCurve(.14f,.3f);
            main.startSize=new ParticleSystem.MinMaxCurve(radius*.22f,radius*.48f);
            main.gravityModifier=.28f; main.maxParticles=32;
            var emission=particles.emission; emission.rateOverTime=0; emission.rateOverDistance=12;
            particles.Play();
        }

        public static ParticleSystem SpawnLiquidImpact(Vector2 position,Vector2 normal,float shotSize,float speed,int order=20,bool onSurface=true)
        {
            normal=normal.sqrMagnitude>.001f?normal.normalized:Vector2.up;
            Vector2 tangent=new Vector2(-normal.y,normal.x);
            float size=Mathf.Clamp(shotSize,.2f,1.6f);
            ParticleSystem particles=CreateParticles("LiquidImpactSpray",null,Color.white,order);
            particles.transform.position=position;
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=GetLiquidMaterial(2);
            var main=particles.main; main.loop=false; main.duration=.55f;
            main.startLifetime=new ParticleSystem.MinMaxCurve(.22f,.55f);
            main.startSize=new ParticleSystem.MinMaxCurve(size*.10f,size*.28f);
            main.gravityModifier=.75f; main.maxParticles=24; main.stopAction=ParticleSystemStopAction.None;
            var emission=particles.emission; emission.enabled=false;
            particles.Play();
            float force=Mathf.Clamp(speed*.16f,1.0f,3.0f);
            int count=Mathf.Clamp(Mathf.RoundToInt(10+size*8),10,23);
            for(int i=0;i<count;i++)
            {
                float fan=Mathf.Lerp(-1.25f,1.25f,i/(float)(count-1));
                Vector2 velocity=(normal*Mathf.Cos(fan)+tangent*Mathf.Sin(fan))*Random.Range(.6f,1.6f)*force;
                particles.Emit(new ParticleSystem.EmitParams{position=position+normal*.025f,velocity=velocity},1);
            }
            WaterEffectPool.ReturnAfter(particles.gameObject,.8f);
            if(!onSurface) return particles;
            bool created;
            var sheet=WaterEffectPool.Rent("LiquidImpactSheet",out created);
            sheet.transform.position=position+normal*(size*.55f+.015f);
            sheet.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(normal.y,normal.x)*Mathf.Rad2Deg-90);
            sheet.transform.localScale=new Vector3(size*2.6f,size*1.6f,1);
            var sprite=created?sheet.AddComponent<SpriteRenderer>():sheet.GetComponent<SpriteRenderer>(); sprite.sprite=GetLiquidQuad();
            sprite.sharedMaterial=GetLiquidMaterial(3); sprite.sortingOrder=order;
            if(created)sheet.AddComponent<WaterImpactSheet>();
            WaterEffectPool.ReturnAfter(sheet,.34f);
            return particles;
        }

        public static Sprite GetDropletSprite()
        {
            if (dropletSprite == null) dropletSprite = Resources.Load<Sprite>("WaterProjectiles/WaterDrop");
            return dropletSprite;
        }

        public static Material GetParticleMaterial()
        {
            if (particleMaterial == null) particleMaterial = Resources.Load<Material>("WaterProjectiles/WaterMaterial");
            return particleMaterial;
        }

        public static Material GetSpriteMaterial()
        {
            if (spriteMaterial == null) spriteMaterial = Resources.Load<Material>("WaterProjectiles/WaterSpriteMaterial");
            return spriteMaterial;
        }

        public static void AddRibbon(Transform parent, Color color, float radius, int sortingOrder)
        {
            GameObject ribbon = new GameObject("WaterRibbon");
            ribbon.transform.SetParent(parent, false);
            TrailRenderer trail = ribbon.AddComponent<TrailRenderer>();
            trail.time = 0.16f;
            trail.startWidth = radius * 1.5f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.025f;
            trail.numCapVertices = 3;
            trail.numCornerVertices = 3;
            trail.sharedMaterial = GetParticleMaterial();
            trail.sortingOrder = sortingOrder;
            trail.startColor = new Color(color.r, color.g, color.b, 0.75f);
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
        }

        private static ParticleSystem CreateParticles(string name, Transform parent, Color color, int sortingOrder)
        {
            bool created = true;
            GameObject obj = parent == null ? WaterEffectPool.Rent(name, out created) : new GameObject(name);
            if (parent != null) obj.transform.SetParent(parent, false);
            if (!created)
            {
                var cached = obj.GetComponent<ParticleSystem>();
                cached.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var cachedMain = cached.main; cachedMain.startColor = color;
                obj.GetComponent<ParticleSystemRenderer>().sortingOrder = sortingOrder;
                return cached;
            }
            ParticleSystem particles = obj.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystemRenderer renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetParticleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = sortingOrder;
            ParticleSystem.MainModule main = particles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.startColor = color;
            main.startSpeed = 0f;
            main.maxParticles = 64;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = false;
            ParticleSystem.ColorOverLifetimeModule fade = particles.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.7f, 0.35f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            return particles;
        }

        public static void AddDropletStream(Transform parent, Color color, float radius, int sortingOrder)
        {
            ParticleSystem particles = CreateParticles("WaterDroplets", parent, color, sortingOrder);
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startSize = new ParticleSystem.MinMaxCurve(radius * 0.35f, radius * 0.8f);
            main.gravityModifier = 0.18f;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 9f;
            particles.Play();
        }

        public static ParticleSystem SpawnSplash(Vector2 position, Color color, float scale, Vector2 normal, int sortingOrder = 20)
        {
            ParticleSystem particles = CreateParticles("WaterSplash", null, color, sortingOrder);
            particles.transform.position = position;
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f * scale, 0.13f * scale);
            main.gravityModifier = 0.45f;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;
            main.stopAction = ParticleSystemStopAction.None;
            particles.Play();
            // Explicit XY velocities prevent the default cone emitting down the camera's Z axis.
            for (int i = 0; i < 16; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 velocity = (direction * Random.Range(0.7f, 2.5f) + normal * 0.9f) * scale;
                ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams { velocity = velocity };
                particles.Emit(emit, 1);
            }
            WaterEffectPool.ReturnAfter(particles.gameObject, 1.1f);
            return particles;
        }

        public static void ReleaseTrail(Transform projectile)
        {
            ParticleSystem[] systems = projectile.GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].transform.SetParent(null, true);
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Object.Destroy(systems[i].gameObject, 0.6f);
            }
            TrailRenderer[] trails = projectile.GetComponentsInChildren<TrailRenderer>();
            for (int i = 0; i < trails.Length; i++)
            {
                trails[i].transform.SetParent(null, true);
                trails[i].emitting = false;
                Object.Destroy(trails[i].gameObject, trails[i].time + 0.05f);
            }
        }
    }
}
