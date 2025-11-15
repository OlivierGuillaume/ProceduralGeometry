using System;
using System.Collections.Generic;
using UnityEngine;

namespace OG.ProceduralGeometry
{
    public partial class ProceduralGeometry
    {
        public readonly HashSet<Face> Faces = new HashSet<Face>();
        public readonly HashSet<Vertex> Vertices = new HashSet<Vertex>();


        public Mesh GenerateMesh(HashSet<int> uvChannels = null)
        {
            if (uvChannels == null) uvChannels = new HashSet<int>() { 0, 1, 2, 3, 4, 5, 6, 7 };

            List<Vector3> vertices = new();
            Dictionary<ValueTuple<Vertex, Face>, int> VerticesIndices = new();
            Dictionary<int, List<Vector2>> uvs = new();
            List<Color> colors = new();

            SetVertices(vertices, VerticesIndices, uvs);

            Dictionary<int, List<int>> submeshesTris = new();
            int subMeshCount = 0;

            foreach (Face face in Faces)
            {
                int submesh = face.submesh;
                if (submesh >= subMeshCount) subMeshCount = submesh + 1;
                if (!submeshesTris.ContainsKey(submesh)) submeshesTris[submesh] = new();

                List<int> tris = submeshesTris[submesh];

                foreach (Vertex v in face.GetTriangles())
                {
                    int i = GetVertexIndice(v, face);
                    tris.Add(i);
                }
            }

            Mesh mesh = new Mesh();

            if (vertices.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.vertices = vertices.ToArray();

            foreach (var kv in uvs)
            {
                mesh.SetUVs(kv.Key, kv.Value);
            }

            mesh.SetColors(colors);

            mesh.subMeshCount = subMeshCount;

            foreach (var kv in submeshesTris)
            {
                mesh.SetTriangles(kv.Value, kv.Key);
            }

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;


            void SetVertices(List<Vector3> verticesOutput, Dictionary<ValueTuple<Vertex, Face>, int> indicesOutput, Dictionary<int, List<Vector2>> uvs)
            {
                foreach (int channel in uvChannels)
                {
                    uvs[channel] = new();
                }

                foreach (Vertex v in Vertices)
                {
                    foreach (HashSet<Face> smoothSurface in GetVertexSurfaces(v))
                    {
                        int vertexIndex = vertices.Count;
                        vertices.Add(v.position);
                        Vector2[] uv = new Vector2[8];
                        Color color = new(0f, 0f, 0f, 0f);

                        foreach (Face face in smoothSurface)
                        {
                            VerticesIndices[new ValueTuple<Vertex, Face>(v, face)] = vertexIndex;

                            foreach (int channel in uvChannels)
                            {
                                ValueTuple<Vertex, int> key = new(v, channel);

                                Vector2 faceuv;

                                if (!face.uvs.TryGetValue(key, out faceuv)) continue;

                                uv[channel] += faceuv;
                            }

                            if (face.colors.TryGetValue(v, out Color col)) color += col;

                        }

                        foreach (int channel in uvChannels)
                        {
                            uv[channel] /= smoothSurface.Count;
                            uvs[channel].Add(uv[channel]);
                        }

                        colors.Add(color / smoothSurface.Count);

                    }
                }
            }

            //Group the vertex faces when they share a smooth edge 
            List<HashSet<Face>> GetVertexSurfaces(Vertex vertex)
            {
                // Dictionary to store the adjacency list of the graph
                Dictionary<Face, HashSet<Face>> adjacencyList = new Dictionary<Face, HashSet<Face>>();

                // Build the graph
                foreach (Edge edge in vertex.edges)
                {
                    if (!edge.smooth) continue;
                    if (edge.faces.Count < 2) continue;

                    Face f0 = edge.faces[0];
                    Face f1 = edge.faces[1];

                    if (!adjacencyList.ContainsKey(f0))
                        adjacencyList[f0] = new();
                    if (!adjacencyList.ContainsKey(f1))
                        adjacencyList[f1] = new();

                    adjacencyList[f0].Add(f1);
                    adjacencyList[f1].Add(f0);
                }

                // List to store the groups
                List<HashSet<Face>> groups = new List<HashSet<Face>>();
                HashSet<Face> visited = new HashSet<Face>();

                // DFS method to explore a group
                void DFS(Face node, HashSet<Face> group)
                {
                    visited.Add(node);
                    group.Add(node);

                    foreach (var neighbor in adjacencyList[node])
                    {
                        if (!visited.Contains(neighbor))
                        {
                            DFS(neighbor, group);
                        }
                    }
                }


                // Find all connected components
                HashSet<Face> group = new HashSet<Face>();
                foreach (var node in adjacencyList.Keys)
                {
                    if (!visited.Contains(node))
                    {
                        group.Clear();
                        DFS(node, group);
                        groups.Add(group);
                    }
                }

                return groups;
            }


            int GetVertexIndice(Vertex vertex, Face face)
            {

                var key = new ValueTuple<Vertex, Face>(vertex, face);
                int index = -1;

                if (!VerticesIndices.TryGetValue(key, out index))
                {
                    index = vertices.Count;
                    VerticesIndices[key] = index;
                    vertices.Add(vertex.position);
                    foreach (int channel in uvChannels)
                    {
                        uvs[channel].Add(face.GetUV(vertex, channel));
                    }
                    colors.Add(face.GetColor(vertex));

                }

                return index;
            }
        }

        public void AddFace(Face face)
        {
            Faces.Add(face);
            foreach (Vertex vertex in face.Vertices)
            {
                Vertices.Add(vertex);
            }
        }

        public void Remove(Face face)
        {
            Faces.Remove(face);
            face.ClearReferences();
        }

        public void Remove(Vertex vertex)
        {
            List<Face> faces = new(vertex.faces);
            foreach (Face face in faces) Remove(face);
            Vertices.Remove(vertex);
        }

        public Face AddFace(params Vertex[] vertices)
        {
            Face face = new Face(vertices);
            AddFace(face);
            return face;
        }

        public HashSet<Vertex> AddGeometry(ProceduralGeometry other)
        {
            Dictionary<Vertex, Vertex> ClonedVertices = new(other.Vertices.Count);

            foreach (Vertex v in other.Vertices) ClonedVertices[v] = new(v.position);

            foreach(Face face in other.Faces)
            {
                Vertex[] vertices = new Vertex[face.VerticesCount];
                for (int i = 0; i < face.VerticesCount; i++)
                {
                    vertices[i] = ClonedVertices[face.vertices[i]];                   
                }
                Face clonedFace = AddFace(vertices);
                for (int i = 0; i < face.VerticesCount; i++)
                {
                    for(int channel = 0; channel < 8; channel++)
                    {
                        clonedFace.SetUV(vertices[i], face.GetUV(face.vertices[i], channel), channel);
                        clonedFace.SetColor(vertices[i], face.GetColor(face.vertices[i]));
                    }
                }
                foreach(Edge e in face.Edges)
                {
                    Vertex v0 = ClonedVertices[e.Vertex0];
                    Vertex v1 = ClonedVertices[e.Vertex1];
                    v0.GetEdgeWith(v1).smooth = e.smooth;
                }
            }           


            return new(ClonedVertices.Values);
        }

        public void Triangulate()
        {
            //Cloning Faces because it will be modified in the loop
            HashSet<Face> faces = new();
            foreach (Face face in Faces) faces.Add(face);

            HashSet<Face> NewFaces = new HashSet<Face>();

            foreach (Face face in faces)
            {
                int n = face.Vertices.Count;

                if (n <= 3) continue;

                var tris = face.GetTriangles();

                NewFaces.Clear();

                for (int i = 0; i < tris.Count - 2; i += 3)
                {
                    Face triangle = AddFace(tris[i], tris[i + 1], tris[i + 2]);
                    NewFaces.Add(triangle);

                    triangle.SetUV(tris[i], face.GetUV(tris[i]));
                    triangle.SetUV(tris[i + 1], face.GetUV(tris[i + 1]));
                    triangle.SetUV(tris[i + 2], face.GetUV(tris[i + 2]));

                    triangle.SetColor(tris[i], face.GetColor(tris[i]));
                    triangle.SetColor(tris[i + 1], face.GetColor(tris[i + 1]));
                    triangle.SetColor(tris[i + 2], face.GetColor(tris[i + 2]));

                    triangle.CopyAttributesFrom(face);
                }

                foreach (Face triangle in NewFaces)
                {
                    foreach (Edge edge in triangle.Edges)
                    {
                        bool insideEdge = true;
                        foreach (Face adjacentFace in edge.Faces)
                        {
                            if (!NewFaces.Contains(adjacentFace))
                            {
                                insideEdge = false;
                                break;
                            }
                        }
                        if (insideEdge) edge.smooth = true;
                    }
                }

                Remove(face);
            }
        }

        public ProceduralGeometry()
        {

        }

        public ProceduralGeometry(ProceduralGeometry geom)
        {
            AddGeometry(geom);
        }


        public ProceduralGeometry(Mesh Mesh)
        {
            Dictionary<Vector3, Vertex> verticesPos = new Dictionary<Vector3, Vertex>();
            Vertex[] vertices = new Vertex[Mesh.vertexCount];
            List<Vector2>[] uvs = new List<Vector2>[8];
            for (int channel = 0; channel <= 7; channel++)
            {
                List<Vector2> uvsi = new();
                Mesh.GetUVs(channel, uvsi);
                uvs[channel] = uvsi;
            }
            List<Color> colors = new List<Color>();
            Mesh.GetColors(colors);

            for (int i = 0; i < Mesh.vertices.Length; i++)
            {
                Vector3 pos = Mesh.vertices[i];

                if (!verticesPos.ContainsKey(pos))
                {
                    verticesPos[pos] = new Vertex(pos);
                }
                vertices[i] = verticesPos[pos];
            }

            for (int submesh = 0; submesh < Mesh.subMeshCount; submesh++)
            {
                var tris = Mesh.GetTriangles(submesh);
                Dictionary<Face, int> facesIndices = new Dictionary<Face, int>();
                Dictionary<ValueTuple<Face, Vertex>, int> VerticesIndices = new();
                for (int i = 0; i < tris.Length; i += 3)
                {
                    Vertex A = vertices[tris[i]];
                    Vertex B = vertices[tris[i + 1]];
                    Vertex C = vertices[tris[i + 2]];

                    Face face = new Face(A, B, C);

                    face.submesh = submesh;

                    AddFace(face);

                    facesIndices[face] = i;

                    VerticesIndices[new(face, A)] = tris[i];
                    VerticesIndices[new(face, B)] = tris[i + 1];
                    VerticesIndices[new(face, C)] = tris[i + 2];


                    foreach (Edge edge in face.Edges)
                    {
                        bool smooth = true;

                        foreach (Face other in edge.faces)
                        {
                            if (other == face) continue;

                            if (
                                VerticesIndices[new(other, edge.Vertex0)] != VerticesIndices[new(face, edge.Vertex0)]
                                || VerticesIndices[new(other, edge.Vertex1)] != VerticesIndices[new(face, edge.Vertex1)]
                                )
                            {
                                smooth = false;
                                break;
                            }
                        }

                        edge.smooth = smooth;
                    }

                    for (int channel = 0; channel <= 7; channel++)
                    {
                        var uvsc = uvs[channel];
                        if (uvsc == null || uvsc.Count == 0) continue;
                        face.SetUV(A, uvs[channel][tris[i]]);
                        face.SetUV(B, uvs[channel][tris[i + 1]]);
                        face.SetUV(C, uvs[channel][tris[i + 2]]);
                    }

                    face.SetColor(A, colors[tris[i]]);
                    face.SetColor(B, colors[tris[i + 1]]);
                    face.SetColor(C, colors[tris[i + 2]]);
                }
            }
        }


    }
}
