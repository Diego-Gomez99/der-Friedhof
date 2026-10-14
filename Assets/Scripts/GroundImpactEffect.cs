using UnityEngine;

// Burst of dust and dirt clods played where an enemy drives its scythe into the ground.
// Everything is built at runtime, so no prefab or scene setup is needed. The object destroys
// itself when the particles are gone.
public class GroundImpactEffect : MonoBehaviour
{
    private const int DustCount = 14;
    private const int ClodCount = 28;
    private const float Lifetime = 2.5f; // Longer than any particle lives.

    private static readonly Color DustColor = new Color(0.32f, 0.28f, 0.23f, 0.55f);
    private static readonly Color ClodLight = new Color(0.22f, 0.17f, 0.12f);
    private static readonly Color ClodDark = new Color(0.09f, 0.07f, 0.05f);

    private static Material dustMaterial;
    private static Material clodMaterial;

    public static void Play(Vector3 position)
    {
        GameObject root = new GameObject("Ground Impact Effect");
        root.transform.position = position;
        root.AddComponent<GroundImpactEffect>();

        CreateDust(root.transform);
        CreateClods(root.transform);
    }

    private void Start()
    {
        Destroy(gameObject, Lifetime);
    }

    // A low cloud that spreads out along the ground, rises a little and fades.
    private static void CreateDust(Transform parent)
    {
        ParticleSystem particles = CreateSystem("Dust", parent, DustMaterial);

        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0.0f, 2.0f * Mathf.PI);
        main.startColor = DustColor;
        main.gravityModifier = -0.05f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.0f, DustCount) });

        // A flat cone: the dust leaves the hole sideways, close to the ground.
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 75.0f;
        shape.radius = 0.1f;
        shape.rotation = new Vector3(-90.0f, 0.0f, 0.0f);

        ParticleSystem.LimitVelocityOverLifetimeModule drag = particles.limitVelocityOverLifetime;
        drag.enabled = true;
        drag.limit = 0.2f;
        drag.dampen = 0.08f;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1.0f, AnimationCurve.Linear(0.0f, 0.6f, 1.0f, 1.8f));

        ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = Fade(new[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.1f), new GradientAlphaKey(0.0f, 1.0f) });

        particles.Play();
    }

    // Small lumps of earth thrown up out of the hole that fall back down.
    private static void CreateClods(Transform parent)
    {
        ParticleSystem particles = CreateSystem("Clods", parent, ClodMaterial);

        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.11f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0.0f, 2.0f * Mathf.PI);
        main.startColor = new ParticleSystem.MinMaxGradient(ClodDark, ClodLight);
        main.gravityModifier = 1.5f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.0f, ClodCount) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35.0f;
        shape.radius = 0.05f;
        shape.rotation = new Vector3(-90.0f, 0.0f, 0.0f);

        ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-6.0f, 6.0f);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = Fade(new[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 0.8f), new GradientAlphaKey(0.0f, 1.0f) });

        particles.Play();
    }

    // A one-shot system that does not loop or play on its own, so it can be configured first.
    private static ParticleSystem CreateSystem(string name, Transform parent, Material material)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);

        ParticleSystem particles = child.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.1f;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0.0f;

        ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return particles;
    }

    private static Gradient Fade(GradientAlphaKey[] alphaKeys)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) }, alphaKeys);
        return gradient;
    }

    // Sprites/Default is in the always included shaders, so it is also available in builds.
    private static Material DustMaterial
    {
        get
        {
            if (dustMaterial == null)
            {
                dustMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = SoftCircle(32) };
            }
            return dustMaterial;
        }
    }

    private static Material ClodMaterial
    {
        get
        {
            if (clodMaterial == null)
            {
                clodMaterial = new Material(Shader.Find("Sprites/Default"));
            }
            return clodMaterial;
        }
    }

    // White circle whose alpha falls off smoothly from the centre to the edge.
    private static Texture2D SoftCircle(int size)
    {
        Texture2D texture = new Texture2D(size, size) { wrapMode = TextureWrapMode.Clamp };
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 offset = new Vector2(x + 0.5f, y + 0.5f) / size * 2.0f - Vector2.one;
                float alpha = Mathf.SmoothStep(0.0f, 1.0f, 1.0f - offset.magnitude);
                texture.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, alpha));
            }
        }
        texture.Apply();
        return texture;
    }
}
