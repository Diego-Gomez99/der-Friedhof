using UnityEngine;

// Puff of black smoke that wraps an enemy while it appears or disappears. It swells quickly,
// so it covers the body by the time the enemy is shown or hidden, then rises and thins out.
// Everything is built at runtime, so no prefab or scene setup is needed. The object destroys
// itself when the particles are gone.
public class SmokeEffect : MonoBehaviour
{
    // Seconds from Play until the cloud is thick enough to hide the switch.
    public const float CoverDelay = 0.2f;

    private const int CloudCount = 60;
    private const int WispCount = 24;
    private const float Lifetime = 3.0f; // Longer than any particle lives.

    private static readonly Color SmokeLight = new Color(0.08f, 0.08f, 0.09f, 0.9f);
    private static readonly Color SmokeDark = new Color(0.0f, 0.0f, 0.0f, 0.95f);

    // Trail from where the last enemy vanished to where the next one appears.
    // Seconds the head of the trail takes to get there. The enemy appears some time after that.
    public const float TrailTravelTime = 0.3f;
    private const float TrailSpacing = 0.25f; // Metres between trail puffs.
    private const int TrailMaxPuffs = 160;
    private const float TrailLifetime = 1.6f; // Longer than any trail puff lives.
    // Once there, the trail keeps puffing in place where the new body will be, until it appears.
    private const float HoldPuffsPerSecond = 30.0f;
    private const int HoldMaxPuffs = 48;
    private static readonly Color TrailColor = new Color(0.0f, 0.0f, 0.0f, 0.45f);

    private static Material smokeMaterial;

    private ParticleSystem trailParticles;
    // Corners of the trail, already lifted, and its total length along them.
    private Vector3[] trailPoints;
    private float trailLength;
    private float trailStartTime;
    private float trailEmittedDistance;
    private float trailHoldTime;
    private float trailHeight;
    private int holdPuffsEmitted;

    // feet is the bottom of the body, height and radius its size in metres.
    public static void Play(Vector3 feet, float height, float radius)
    {
        GameObject root = new GameObject("Smoke Effect");
        root.transform.position = feet;
        root.AddComponent<SmokeEffect>();

        CreateCloud(root.transform, height, radius);
        CreateWisps(root.transform, height, radius);
    }

    // A faint thread of black smoke that runs low over the ground from one body's feet to
    // another's, as if the enemy slid there unseen. height is the body's height in metres.
    // Once it gets there, it lingers in place for holdTime seconds.
    public static void PlayTrail(Vector3 fromFeet, Vector3 toFeet, float height, float holdTime)
    {
        PlayTrail(new[] { fromFeet, toFeet }, height, holdTime);
    }

    // The same trail along a path with corners, through each of feetPoints in turn.
    public static void PlayTrail(Vector3[] feetPoints, float height, float holdTime)
    {
        Vector3 lift = Vector3.up * height * 0.35f;

        GameObject root = new GameObject("Smoke Trail");
        root.transform.position = feetPoints[0] + lift;
        SmokeEffect effect = root.AddComponent<SmokeEffect>();
        effect.trailPoints = new Vector3[feetPoints.Length];
        for (int i = 0; i < feetPoints.Length; i++)
        {
            effect.trailPoints[i] = feetPoints[i] + lift;
            if (i > 0)
            {
                effect.trailLength += Vector3.Distance(effect.trailPoints[i - 1], effect.trailPoints[i]);
            }
        }
        effect.trailStartTime = Time.time;
        effect.trailHoldTime = holdTime;
        effect.trailHeight = height;
        effect.trailParticles = CreateTrail(root.transform, height);
    }

    private void Start()
    {
        Destroy(gameObject, trailParticles != null ? TrailTravelTime + trailHoldTime + TrailLifetime : Lifetime);
    }

    // Moves the head of the trail and leaves puffs evenly along the stretch it covered this frame.
    // Then puffs around the end of the trail while it lingers there.
    private void Update()
    {
        if (trailParticles == null) return;

        float length = trailLength;
        if (trailEmittedDistance >= length)
        {
            EmitHoldPuffs();
            return;
        }

        // Starts fast and slows down as it reaches the new body.
        float t = Mathf.Clamp01((Time.time - trailStartTime) / TrailTravelTime);
        float headDistance = length * (1.0f - (1.0f - t) * (1.0f - t));

        // Very long trails get sparser puffs instead of more of them.
        float spacing = Mathf.Max(TrailSpacing, length / TrailMaxPuffs);
        ParticleSystem.EmitParams puff = new ParticleSystem.EmitParams();
        while (trailEmittedDistance + spacing <= headDistance || (t >= 1.0f && trailEmittedDistance < length))
        {
            trailEmittedDistance = Mathf.Min(trailEmittedDistance + spacing, length);
            puff.position = TrailPointAt(trailEmittedDistance) + Random.insideUnitSphere * 0.1f;
            puff.velocity = Random.insideUnitSphere * 0.15f + Vector3.up * 0.1f;
            trailParticles.Emit(puff, 1);
        }
        transform.position = TrailPointAt(headDistance);
    }

    // The point this many metres along the trail, from its start.
    private Vector3 TrailPointAt(float distance)
    {
        for (int i = 1; i < trailPoints.Length; i++)
        {
            float segment = Vector3.Distance(trailPoints[i - 1], trailPoints[i]);
            if (distance <= segment && segment > 0.0f)
            {
                return Vector3.Lerp(trailPoints[i - 1], trailPoints[i], distance / segment);
            }
            distance -= segment;
        }
        return trailPoints[trailPoints.Length - 1];
    }

    // Puffs at a steady rate around the end of the trail, from when it arrives until holdTime later.
    private void EmitHoldPuffs()
    {
        float holding = Mathf.Min(Time.time - trailStartTime - TrailTravelTime, trailHoldTime);
        int due = Mathf.Min(Mathf.FloorToInt(holding * HoldPuffsPerSecond), HoldMaxPuffs);
        ParticleSystem.EmitParams puff = new ParticleSystem.EmitParams();
        for (; holdPuffsEmitted < due; holdPuffsEmitted++)
        {
            puff.position = trailPoints[trailPoints.Length - 1] + Random.insideUnitSphere * trailHeight * 0.15f;
            puff.velocity = Random.insideUnitSphere * 0.15f + Vector3.up * 0.15f;
            trailParticles.Emit(puff, 1);
        }
    }

    // Thick puffs filling the whole body, which swell, drift up and fade.
    private static void CreateCloud(Transform parent, float height, float radius)
    {
        ParticleSystem particles = CreateSystem("Cloud", parent);

        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(radius * 2.0f, radius * 3.5f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0.0f, 2.0f * Mathf.PI);
        main.startColor = new ParticleSystem.MinMaxGradient(SmokeDark, SmokeLight);
        main.gravityModifier = -0.08f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.0f, CloudCount) });

        // A box as tall as the body: the puffs start all over it and push slightly outward.
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = new Vector3(0.0f, height * 0.5f, 0.0f);
        shape.scale = new Vector3(radius * 1.6f, height, radius * 1.6f);
        shape.randomDirectionAmount = 1.0f;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1.0f, AnimationCurve.EaseInOut(0.0f, 0.4f, 1.0f, 1.6f));

        ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);

        // Opaque early, so the body is covered at CoverDelay, then a slow fade.
        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = Fade(new[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.12f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0.0f, 1.0f) });

        particles.Play();
    }

    // Thin trails that curl up out of the cloud and outlast it.
    private static void CreateWisps(Transform parent, float height, float radius)
    {
        ParticleSystem particles = CreateSystem("Wisps", parent);

        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(radius * 0.6f, radius * 1.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0.0f, 2.0f * Mathf.PI);
        main.startColor = new ParticleSystem.MinMaxGradient(SmokeDark, SmokeLight);
        main.gravityModifier = -0.15f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.05f, WispCount) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 30.0f;
        shape.radius = radius;
        shape.position = new Vector3(0.0f, height * 0.3f, 0.0f);
        shape.rotation = new Vector3(-90.0f, 0.0f, 0.0f);

        // A sideways sway, so the trails curl instead of going straight up.
        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.6f;
        noise.frequency = 0.8f;
        noise.scrollSpeed = 0.5f;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1.0f, AnimationCurve.Linear(0.0f, 0.6f, 1.0f, 1.8f));

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = Fade(new[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.8f, 0.15f), new GradientAlphaKey(0.0f, 1.0f) });

        particles.Play();
    }

    // Small, thin puffs emitted by hand from Update, which swell a little, sway and fade.
    private static ParticleSystem CreateTrail(Transform parent, float height)
    {
        ParticleSystem particles = CreateSystem("Trail", parent);

        ParticleSystem.MainModule main = particles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(height * 0.15f, height * 0.3f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0.0f, 2.0f * Mathf.PI);
        main.startColor = TrailColor;
        main.gravityModifier = -0.03f;
        main.maxParticles = TrailMaxPuffs + HoldMaxPuffs + 16;
        // Keeps playing, so the puffs emitted after the first 0.1 s are simulated too.
        main.loop = true;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.4f;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1.0f, AnimationCurve.EaseInOut(0.0f, 0.6f, 1.0f, 1.5f));

        ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = Fade(new[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.15f), new GradientAlphaKey(0.0f, 1.0f) });

        particles.Play();
        return particles;
    }

    // A one-shot system that does not loop or play on its own, so it can be configured first.
    private static ParticleSystem CreateSystem(string name, Transform parent)
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
        main.maxParticles = 96;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0.0f;

        ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
        renderer.material = SmokeMaterial;
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
    private static Material SmokeMaterial
    {
        get
        {
            if (smokeMaterial == null)
            {
                smokeMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = SoftPuff(64) };
            }
            return smokeMaterial;
        }
    }

    // White puff whose alpha falls off from the centre and is broken up by a little noise,
    // so overlapping puffs read as smoke rather than as round blobs.
    private static Texture2D SoftPuff(int size)
    {
        Texture2D texture = new Texture2D(size, size) { wrapMode = TextureWrapMode.Clamp };
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 offset = new Vector2(x + 0.5f, y + 0.5f) / size * 2.0f - Vector2.one;
                float noise = Mathf.PerlinNoise(x * 0.12f, y * 0.12f) * 0.5f + Mathf.PerlinNoise(x * 0.3f + 7.0f, y * 0.3f + 3.0f) * 0.5f;
                float falloff = Mathf.SmoothStep(0.0f, 1.0f, 1.0f - offset.magnitude);
                float alpha = Mathf.Clamp01(Mathf.Sqrt(falloff) * (0.7f + 0.8f * noise));
                texture.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, alpha));
            }
        }
        texture.Apply();
        return texture;
    }
}
