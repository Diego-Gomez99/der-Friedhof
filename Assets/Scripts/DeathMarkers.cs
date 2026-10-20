using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Remembers where each killer's scythe ended up planted in the ground, and every time that scene
// loads again leaves a copy of the scythe there, with no collider, and the mound of earth around
// it, to mark where the player died.
// The list is static, so it lasts until the game is closed, including trips through the main menu.
public static class DeathMarkers
{
    private struct Marker
    {
        public string sceneName;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Vector3 groundPoint;
        public int groundSeed;
    }

    private static readonly List<Marker> markers = new List<Marker>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        markers.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // groundPoint is where the handle enters the ground, and groundSeed the seed its ScytheGroundMark was built from.
    public static void Record(Transform weapon, Vector3 groundPoint, int groundSeed)
    {
        markers.Add(new Marker
        {
            sceneName = weapon.gameObject.scene.name,
            position = weapon.position,
            rotation = weapon.rotation,
            scale = weapon.lossyScale,
            groundPoint = groundPoint,
            groundSeed = groundSeed,
        });
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!markers.Exists(marker => marker.sceneName == scene.name)) return;

        // The mesh and materials come from an enemy's scythe in the loaded scene.
        Enemy enemy = Object.FindAnyObjectByType<Enemy>();
        Transform template = enemy != null ? enemy.transform.Find("weapon") : null;
        MeshFilter templateMesh = template != null ? template.GetComponent<MeshFilter>() : null;
        MeshRenderer templateRenderer = template != null ? template.GetComponent<MeshRenderer>() : null;
        if (templateMesh == null || templateRenderer == null) return;

        foreach (Marker marker in markers)
        {
            if (marker.sceneName != scene.name) continue;

            GameObject scythe = new GameObject("Death Marker");
            SceneManager.MoveGameObjectToScene(scythe, scene);
            scythe.transform.SetPositionAndRotation(marker.position, marker.rotation);
            scythe.transform.localScale = marker.scale;
            scythe.AddComponent<MeshFilter>().sharedMesh = templateMesh.sharedMesh;
            MeshRenderer meshRenderer = scythe.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = templateRenderer.sharedMaterials;
            meshRenderer.shadowCastingMode = templateRenderer.shadowCastingMode;
            meshRenderer.receiveShadows = templateRenderer.receiveShadows;

            SceneManager.MoveGameObjectToScene(ScytheGroundMark.Create(marker.groundPoint, marker.groundSeed, false), scene);
        }
    }
}
