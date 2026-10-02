using UnityEngine;

namespace TapGJ.WaterProjectiles
{
    /// <summary>Procedural Shuriken particles confined to the game's XY plane.</summary>
    public static class WaterVfx
    {
        private static Sprite dropletSprite;
        private static Material particleMaterial;
        private static Material spriteMaterial;

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
            GameObject obj = new GameObject(name);
            if (parent != null) obj.transform.SetParent(parent, false);
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
            main.stopAction = ParticleSystemStopAction.Destroy;
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
            Object.Destroy(particles.gameObject, 1.1f);
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
