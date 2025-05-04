using System.Collections.Generic;
using UnityEngine;

namespace OG.ProceduralGeometry
{
    public partial class ProceduralGeometry
    {
        /// <summary>
        /// Extrude toward direction
        /// </summary>
        /// <returns>A list of faces created by the extrusion.</returns>
        public HashSet<Face> Extrude(HashSet<Face> Faces, Vector3 direction)
        {
            HashSet<Edge> inverted = new HashSet<Edge>();
            HashSet<Edge> outsideEdges = GetOutsideEdges(inverted, Faces);

            Dictionary<Vertex, Vertex> SplittedVertices = new();

            SplitVertices(outsideEdges, SplittedVertices);

            SplitFaces(Faces, SplittedVertices);

            MoveFaces(Faces, direction);           

            return CreateOutsideFaces(outsideEdges, SplittedVertices, inverted);

            void MoveFaces(HashSet<Face> Faces, Vector3 direction)
            {
                HashSet<Vertex> ExtrudedVertices = new HashSet<Vertex>();

                foreach (Face face in Faces)
                {
                    foreach (Vertex vertex in face.Vertices)
                    {
                        ExtrudedVertices.Add(vertex);
                    }
                }

                foreach (Vertex vertex in ExtrudedVertices)
                {
                    vertex.position += direction;
                }
            }
        }

        /// <summary>
        /// Extrude along normals
        /// </summary>
        /// <returns>A list of faces created by the extrusion.</returns>
        public HashSet<Face> ExtrudeAlongNormals(HashSet<Face> Faces, float distance)
        {
            HashSet<Edge> inverted = new HashSet<Edge>();
            HashSet<Edge> outsideEdges = GetOutsideEdges(inverted, Faces);

            Dictionary<Vertex, Vertex> SplittedVertices = new();

            SplitVertices(outsideEdges, SplittedVertices);

            SplitFaces(Faces, SplittedVertices);

            MoveFaces(Faces, distance);

            return CreateOutsideFaces(outsideEdges, SplittedVertices, inverted);

            void MoveFaces(HashSet<Face> Faces, float distance)
            {
                Dictionary<Vertex, Vector3> normals = new Dictionary<Vertex, Vector3>();

                foreach (Face face in Faces)
                {
                    foreach (Vertex vertex in face.Vertices)
                    {
                        if (normals.ContainsKey(vertex)) normals[vertex] += face.Normal;
                        else normals[vertex] = face.Normal;
                    }
                }

                foreach (var kv in normals)
                {
                    kv.Key.position += distance * kv.Value.normalized;
                }
            }
        }

        private static HashSet<Edge> GetOutsideEdges(HashSet<Edge> inverted, HashSet<Face> Faces)
        {
            HashSet<Edge> Edges = new HashSet<Edge>();

            foreach (Face face in Faces)
            {
                foreach (Edge edge in face.Edges)
                {
                    foreach (Face otherFace in edge.faces)
                    {
                        if (Faces.Contains(otherFace)) continue;
                        Edges.Add(edge);

                        for (int i = 0; i < face.Vertices.Count; i++)
                        {
                            Vertex v0 = face.Vertices[i];
                            if (v0 == edge.Vertex0)
                            {
                                if (face.Vertices[(i + 1) % face.Vertices.Count] != edge.Vertex1)
                                {
                                    inverted.Add(edge);
                                }
                                break;
                            }
                        }


                        break;
                    }
                }
            }

            return Edges;
        }

        private static void SplitFaces(HashSet<Face> Faces, Dictionary<Vertex, Vertex> SplittedVertices)
        {
            foreach (Face face in Faces)
            {
                Vertex[] newVertices = new Vertex[face.VerticesCount];
                var oldVertices = face.Vertices;

                for (int i = 0; i < face.VerticesCount; i++)
                {
                    Vertex v = oldVertices[i];

                    if (SplittedVertices.ContainsKey(v))
                    {
                        v = SplittedVertices[v];
                    }

                    newVertices[i] = v;
                }

                face.SetVertices(newVertices);
            }
        }

        private static void SplitVertices(HashSet<Edge> outsideEdges, Dictionary<Vertex, Vertex> SplittedVertices)
        {
            foreach (Edge edge in outsideEdges)
            {
                if (!SplittedVertices.ContainsKey(edge.Vertex0))
                {
                    SplittedVertices[edge.Vertex0] = new Vertex(edge.Vertex0.position);
                }
                if (!SplittedVertices.ContainsKey(edge.Vertex1))
                {
                    SplittedVertices[edge.Vertex1] = new Vertex(edge.Vertex1.position);
                }
            }
        }

        private HashSet<Face> CreateOutsideFaces(HashSet<Edge> outsideEdges, Dictionary<Vertex, Vertex> SplittedVertices, HashSet<Edge> inverted)
        {
            HashSet<Face> faces = new HashSet<Face>();

            foreach (Edge edge in outsideEdges)
            {
                bool inv = inverted.Contains(edge);

                Vertex v0 = inv ? edge.Vertex1 : edge.Vertex0;
                Vertex v1 = inv ? edge.Vertex0 : edge.Vertex1;
                Vertex v2 = SplittedVertices[v1];
                Vertex v3 = SplittedVertices[v0];

                Face face = new Face(v0, v1, v2, v3);

                faces.Add(face);
                AddFace(face);
            }

            return faces;
        }

    }
}
