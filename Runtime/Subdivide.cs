using System.Collections.Generic;
using UnityEditor.Hardware;
using UnityEngine;

namespace OG.ProceduralGeometry
{
    public partial class ProceduralGeometry
    {
        /// <summary>
        /// Triangulates then subdivide, new vertices are positioned in the triangle plane.
        /// </summary>
        public void BasicSubdivide(int subdivisions)
        {
            Triangulate();

            for (int sub = 0; sub < subdivisions; sub++)
            {
                HashSet<Face> faces = new(Faces);
                Dictionary<Edge, Vertex> newVertices = new();

                Vertex[] CenterTriVertices = new Vertex[3];
                Vector2[] CenterTriUVs = new Vector2[3];
                Color[] CenterTriColors = new Color[3];

                foreach (Face face in faces)
                {

                    if(face.edges.Count != 3)
                    {
                        throw new System.Exception("Geometry wasn't correctly triangulated");
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        Edge edge = face.Edges[i];
                        if (!newVertices.TryGetValue(edge, out Vertex v)){
                            v = new(0.5f * (edge.Vertex0.position + edge.Vertex1.position));
                            newVertices[edge] = v;
                        }
                        CenterTriVertices[i] = v;
                        CenterTriUVs[i] = (face.GetUV(edge.Vertex0) + face.GetUV(edge.Vertex1)) / 2f;
                        CenterTriColors[i] = (face.GetColor(edge.Vertex0) + face.GetColor(edge.Vertex1)) / 2f;
                    }

                    Face newFace = new(CenterTriVertices);
                    for (int i = 0; i < 3; i++)
                    {
                        newFace.SetUV(CenterTriVertices[i], CenterTriUVs[i]);
                        newFace.SetColor(CenterTriVertices[i], CenterTriColors[i]);
                    }
                    newFace.CopyAttributesFrom(face);
                    AddFace(newFace);

                    for (int i = 0; i < 3; i++)
                    {
                        Edge AB = face.edges[i];
                        Edge BC = face.edges[(i+1)%3];

                        Vertex a = CenterTriVertices[i];
                        Vertex B = AB.GetSharedVertexWith(BC);
                        Vertex c = CenterTriVertices[(i+1)%3];

                        Vector2 uva = CenterTriUVs[i];
                        Vector2 uvB = face.GetUV(B);
                        Vector2 uvc = CenterTriUVs[(i + 1) % 3];

                        Color colora = CenterTriColors[i];
                        Color colorB = face.GetColor(B);
                        Color colorc = CenterTriColors[(i + 1) % 3];

                        if (B == null)
                        {
                            Debug.LogError("A subdivided triangle couldn't get created");
                            continue;
                        }

                        newFace = new(a, B, c);

                        newFace.SetUV(a, uva);
                        newFace.SetUV(B, uvB);
                        newFace.SetUV(c, uvc);

                        newFace.SetColor(a, colora);
                        newFace.SetColor(B, colorB);
                        newFace.SetColor(c, colorc);

                        newFace.CopyAttributesFrom(face);
                        AddFace(newFace);
                    }

                    Remove(face);
                }
            }
        }
    }
}
