using OG.ProceduralGeometry;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    public MeshFilter meshFilter;
    void Start()
    {
        if (meshFilter == null) return;

        ProceduralGeometry Geom = ProceduralGeometry.CubeSphere(20f, 10);

        HashSet<Face> IslandFaces = new();
        HashSet<Face> SeaFaces = new();

        Geom.MarchingTriangles(vertex => vertex.position.y - 15f, out SeaFaces, out IslandFaces, out _);

        HashSet<Face> CliffFaces = Geom.Extrude(IslandFaces, 2f * Vector3.up);

        Geom.AutoSmooth();

        foreach (Face face in IslandFaces) face.SetUV(new Vector2(0.25f, 0.75f));
        foreach (Face face in CliffFaces) face.SetUV(new Vector2(0.25f, 0.25f));
        foreach (Face face in SeaFaces) face.SetUV(new Vector2(0.75f, 0.75f));

        meshFilter.mesh = Geom.GenerateMesh();
    }
}
