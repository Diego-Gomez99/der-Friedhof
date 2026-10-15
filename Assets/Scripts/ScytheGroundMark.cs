using System.Collections.Generic;
using UnityEngine;

// Mound of disturbed earth left around a scythe planted in the ground: a lumpy ridge pushed up
// around the handle, a funnel where it goes in, and a few clods lying around. It stays until the
// scene unloads. The mesh is built at runtime from a seed, so the same seed gives the same mound
// when DeathMarkers rebuilds it. The surface uses the terrain layer under it, darkened, with the
// same tiling, and follows the terrain's slope; elsewhere it is plain dark earth. URP/Lit is used
// so the mound is lit and fogged like the ground around it.
public class ScytheGroundMark : MonoBehaviour
{
    private const float HoleRadius = 0.05f;
    private const float HoleDepth = 0.06f;
    private const float OuterRadius = 0.45f;
    // Fraction of the way out from the hole where the ridge is highest, and its height range.
    private const float RidgeAt = 0.2f;
    private const float RidgeMinHeight = 0.05f;
    private const float RidgeMaxHeight = 0.11f;
    // The outer edge sinks below the ground, so no gap shows on uneven ground.
    private const float EdgeSink = 0.03f;
    private const int Segments = 48;
    private const int Rings = 12;
    private const int ClodCount = 9;
    private const float GrowTime = 0.2f;

    private static readonly Color PlainEarth = new Color(0.16f, 0.12f, 0.09f);
    // Turned earth is darker than the surface it came out of.
    private const float TextureDarkening = 0.6f;

    private static readonly Dictionary<Texture, Material> texturedMaterials = new Dictionary<Texture, Material>();
    private static Material plainMaterial;

    private float growth = 1.0f;

    // groundPoint is where the handle enters the ground. With grow, the mound heaves up out of
    // the ground instead of appearing at once.
    public static GameObject Create(Vector3 groundPoint, int seed, bool grow)
    {
        GameObject mark = new GameObject("Scythe Ground Mark");
        mark.transform.position = groundPoint;

        Terrain terrain = TerrainUnder(groundPoint);
        TerrainLayer layer = terrain != null ? DominantLayer(terrain, groundPoint) : null;

        System.Random random = new System.Random(seed);
        Mesh mesh = BuildMesh(random, groundPoint, terrain, layer);
        mark.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer meshRenderer = mark.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = layer != null && layer.diffuseTexture != null ? TexturedMaterial(layer.diffuseTexture) : PlainMaterial;

        if (grow)
        {
            ScytheGroundMark growing = mark.AddComponent<ScytheGroundMark>();
            growing.growth = 0.0f;
            mark.transform.localScale = new Vector3(1.0f, 0.0f, 1.0f);
        }
        return mark;
    }

    private void Update()
    {
        growth = Mathf.MoveTowards(growth, 1.0f, Time.deltaTime / GrowTime);
        float eased = 1.0f - (1.0f - growth) * (1.0f - growth);
        transform.localScale = new Vector3(1.0f, eased, 1.0f);
        if (growth >= 1.0f) Destroy(this);
    }

    private static Mesh BuildMesh(System.Random random, Vector3 origin, Terrain terrain, TerrainLayer layer)
    {
        Shape shape = new Shape(random);
        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        // Centre of the funnel, then rings from the hole's rim outwards.
        vertices.Add(new Vector3(0.0f, -HoleDepth, 0.0f));
        for (int ring = 0; ring < Rings; ring++)
        {
            float t = ring / (Rings - 1.0f);
            for (int segment = 0; segment < Segments; segment++)
            {
                float angle = segment * 2.0f * Mathf.PI / Segments;
                float radius = Mathf.Lerp(HoleRadius, OuterRadius * shape.Outline(angle), t);
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0.0f, Mathf.Sin(angle) * radius);
                float lump = ring == 0 || ring == Rings - 1 ? 0.0f : (float)(random.NextDouble() - 0.5) * 0.015f;
                offset.y = shape.Height(angle, t) + lump + Slope(terrain, origin, offset);
                vertices.Add(offset);
            }
        }

        for (int segment = 0; segment < Segments; segment++)
        {
            int next = (segment + 1) % Segments;
            triangles.Add(0);
            triangles.Add(1 + next);
            triangles.Add(1 + segment);
        }
        for (int ring = 0; ring < Rings - 1; ring++)
        {
            int inner = 1 + ring * Segments;
            int outer = inner + Segments;
            for (int segment = 0; segment < Segments; segment++)
            {
                int next = (segment + 1) % Segments;
                triangles.Add(inner + segment);
                triangles.Add(inner + next);
                triangles.Add(outer + segment);
                triangles.Add(inner + next);
                triangles.Add(outer + next);
                triangles.Add(outer + segment);
            }
        }

        Mesh mound = new Mesh();
        mound.SetVertices(vertices);
        mound.SetTriangles(triangles, 0);
        mound.RecalculateNormals();

        var parts = new List<CombineInstance> { new CombineInstance { mesh = mound, transform = Matrix4x4.identity } };
        for (int i = 0; i < ClodCount; i++)
        {
            float angle = (float)random.NextDouble() * 2.0f * Mathf.PI;
            float t = Mathf.Lerp(0.3f, 1.3f, (float)random.NextDouble());
            float radius = Mathf.Lerp(HoleRadius, OuterRadius * shape.Outline(angle), t);
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0.0f, Mathf.Sin(angle) * radius);
            float size = Mathf.Lerp(0.025f, 0.06f, (float)random.NextDouble());
            // Partly sunk into whatever it rests on: the mound, or the ground where the mound is lower.
            float surface = t < 1.0f ? Mathf.Max(shape.Height(angle, t), 0.0f) : 0.0f;
            offset.y = surface + Slope(terrain, origin, offset) - size * 0.2f;
            Matrix4x4 place = Matrix4x4.TRS(offset, Quaternion.Euler(0.0f, angle * Mathf.Rad2Deg, 0.0f),
                new Vector3(size, size * 0.65f, size * Mathf.Lerp(0.8f, 1.3f, (float)random.NextDouble())));
            parts.Add(new CombineInstance { mesh = Clod(random), transform = place });
        }

        Mesh mesh = new Mesh { name = "Scythe Ground Mark" };
        mesh.CombineMeshes(parts.ToArray(), true, true);

        // Same tiling as the terrain layer, so the texture lines up with the ground around it.
        Vector3[] combined = mesh.vertices;
        var uvs = new Vector2[combined.Length];
        Vector2 tileSize = layer != null ? layer.tileSize : Vector2.one;
        Vector3 terrainOrigin = terrain != null ? terrain.GetPosition() : Vector3.zero;
        Vector2 tileOffset = layer != null ? layer.tileOffset : Vector2.zero;
        for (int i = 0; i < combined.Length; i++)
        {
            Vector3 world = origin + combined[i] - terrainOrigin;
            uvs[i] = new Vector2((world.x + tileOffset.x) / tileSize.x, (world.z + tileOffset.y) / tileSize.y);
        }
        mesh.uv = uvs;
        mesh.RecalculateBounds();
        return mesh;
    }

    // Lumpy, flat-shaded rock of earth around the origin with radius about 1.
    private static Mesh Clod(System.Random random)
    {
        float g = (1.0f + Mathf.Sqrt(5.0f)) / 2.0f;
        Vector3[] corners =
        {
            new Vector3(-1, g, 0), new Vector3(1, g, 0), new Vector3(-1, -g, 0), new Vector3(1, -g, 0),
            new Vector3(0, -1, g), new Vector3(0, 1, g), new Vector3(0, -1, -g), new Vector3(0, 1, -g),
            new Vector3(g, 0, -1), new Vector3(g, 0, 1), new Vector3(-g, 0, -1), new Vector3(-g, 0, 1),
        };
        for (int i = 0; i < corners.Length; i++)
        {
            corners[i] = corners[i].normalized * Mathf.Lerp(0.7f, 1.15f, (float)random.NextDouble());
        }
        int[] faces =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };

        // Each face gets its own vertices, so the normals are flat.
        var vertices = new Vector3[faces.Length];
        var triangles = new int[faces.Length];
        for (int i = 0; i < faces.Length; i++)
        {
            vertices[i] = corners[faces[i]];
            triangles[i] = i;
        }
        Mesh mesh = new Mesh { vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        return mesh;
    }

    // Height of the ground at offset from the origin, relative to the origin, so the mound follows
    // the terrain's slope while staying anchored where the handle goes in.
    private static float Slope(Terrain terrain, Vector3 origin, Vector3 offset)
    {
        if (terrain == null) return 0.0f;
        Vector3 point = origin + new Vector3(offset.x, 0.0f, offset.z);
        return terrain.SampleHeight(point) - terrain.SampleHeight(origin);
    }

    private static Terrain TerrainUnder(Vector3 point)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            Vector3 local = point - terrain.GetPosition();
            Vector3 size = terrain.terrainData.size;
            if (local.x >= 0.0f && local.z >= 0.0f && local.x <= size.x && local.z <= size.z)
            {
                // Only if the terrain is the ground there, not something lower under a floor.
                float height = terrain.SampleHeight(point) + terrain.GetPosition().y;
                if (Mathf.Abs(height - point.y) < 0.3f) return terrain;
            }
        }
        return null;
    }

    private static TerrainLayer DominantLayer(Terrain terrain, Vector3 point)
    {
        TerrainData data = terrain.terrainData;
        TerrainLayer[] layers = data.terrainLayers;
        if (layers.Length == 0) return null;

        Vector3 local = point - terrain.GetPosition();
        int x = Mathf.Clamp(Mathf.FloorToInt(local.x / data.size.x * data.alphamapWidth), 0, data.alphamapWidth - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(local.z / data.size.z * data.alphamapHeight), 0, data.alphamapHeight - 1);
        float[,,] weights = data.GetAlphamaps(x, z, 1, 1);
        int best = 0;
        for (int i = 1; i < layers.Length && i < weights.GetLength(2); i++)
        {
            if (weights[0, 0, i] > weights[0, 0, best]) best = i;
        }
        return layers[best];
    }

    private static Material TexturedMaterial(Texture texture)
    {
        if (!texturedMaterials.TryGetValue(texture, out Material material) || material == null)
        {
            material = NewMaterial(new Color(TextureDarkening, TextureDarkening, TextureDarkening));
            material.SetTexture("_BaseMap", texture);
            material.mainTexture = texture;
            texturedMaterials[texture] = material;
        }
        return material;
    }

    private static Material PlainMaterial
    {
        get
        {
            if (plainMaterial == null) plainMaterial = NewMaterial(PlainEarth);
            return plainMaterial;
        }
    }

    // URP/Lit is used by the scene's own materials, so it is in builds too.
    private static Material NewMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = "Scythe Ground Mark" };
        material.SetColor("_BaseColor", color);
        material.color = color;
        material.SetFloat("_Smoothness", 0.05f);
        material.SetFloat("_Glossiness", 0.05f);
        return material;
    }

    // Random outline and ridge height around the hole, as a few smooth waves.
    private struct Shape
    {
        private readonly float[] outlinePhases;
        private readonly float[] ridgePhases;
        private readonly float ridgeHeight;

        public Shape(System.Random random)
        {
            outlinePhases = new float[3];
            ridgePhases = new float[3];
            for (int i = 0; i < 3; i++)
            {
                outlinePhases[i] = (float)random.NextDouble() * 2.0f * Mathf.PI;
                ridgePhases[i] = (float)random.NextDouble() * 2.0f * Mathf.PI;
            }
            ridgeHeight = Mathf.Lerp(RidgeMinHeight, RidgeMaxHeight, (float)random.NextDouble());
        }

        // Multiplier of OuterRadius at that angle.
        public float Outline(float angle)
        {
            return 1.0f + 0.2f * Mathf.Sin(2.0f * angle + outlinePhases[0])
                        + 0.12f * Mathf.Sin(5.0f * angle + outlinePhases[1])
                        + 0.06f * Mathf.Sin(9.0f * angle + outlinePhases[2]);
        }

        // Height above the ground at that angle, t of the way from the hole's rim to the edge.
        public float Height(float angle, float t)
        {
            float wave = 0.65f + 0.25f * Mathf.Sin(3.0f * angle + ridgePhases[0])
                               + 0.15f * Mathf.Sin(7.0f * angle + ridgePhases[1])
                               + 0.08f * Mathf.Sin(11.0f * angle + ridgePhases[2]);
            float ridge = ridgeHeight * wave;
            if (t < RidgeAt)
            {
                // From the rim, a little below the ground, up to the crest.
                return Mathf.Lerp(-HoleDepth * 0.4f, ridge, Mathf.SmoothStep(0.0f, 1.0f, t / RidgeAt));
            }
            float fall = 1.0f - Mathf.InverseLerp(RidgeAt, 1.0f, t);
            return ridge * fall * fall - EdgeSink * Mathf.Pow(1.0f - fall, 2.0f);
        }
    }
}
