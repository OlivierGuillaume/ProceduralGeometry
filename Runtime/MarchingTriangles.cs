using System;
using System.Collections.Generic;
using UnityEngine;

namespace OG.ProceduralGeometry
{
    public partial class ProceduralGeometry
    {
        public delegate float ValueGetter(Vertex v);

        readonly static int[][] MarchingTrianglesEdges = { new int[] { }, new int[] { 0, 2 }, new int[] { 1, 0 }, new int[] { 2, 1 }, new int[] { 2, 1 }, new int[] { 1, 0 }, new int[] { 0, 2 }, new int[] { } };

        /// <summary>
        /// Triangulate the faces, estimate the value for each vertex, then:<br />
        /// - The faces which vertices have all negative values are added to NegativeFaces<br />
        /// - The faces which vertices have all positive or null values are added to PositiveFaces<br />
        /// - The faces which vertices with negative values and vertices with positive values are split into a triangle and a quad. These faces are added to the appropriate sets.
        /// </summary>
        /// <param name="minIslandVertices">If > 0, don't create islands with a number of vertices lower than this number. Vertices are counter in the original geometry after triangulation, vertices created by splitting faces are not counted (splitting happens afterward)</param>
        /// <param name="smallValue">a small positive value used to replace the values of removed islands</param>
        public void MarchingTriangles(ValueGetter ValueGetter, out HashSet<Face> NegativeFaces, out HashSet<Face> PositiveFaces, out HashSet<Vertex> NewVertices, bool smoothEdges = false, int minIslandVertices = 0, float smallValue = 0.001f)
        {
            Triangulate();

            NegativeFaces = new HashSet<Face>();
            PositiveFaces = new HashSet<Face>();
            NewVertices = new HashSet<Vertex>();

            Dictionary<Vertex, float> Values = new Dictionary<Vertex, float>();

            foreach (Vertex v in Vertices) Values[v] = ValueGetter(v);

            RemoveSmallIslands();

            //Vertices in between 2 corners that are at different heights
            Dictionary<ValueTuple<Vertex, Vertex>, Vertex> TriangeSideVertices = new();

            //Cloning Faces because it will be modified
            HashSet<Face> faces = new HashSet<Face>();
            foreach (Face face in Faces) faces.Add(face);


            foreach (Face face in faces)
            {
                bool[] positive = new bool[3];
                bool allPos = true;
                bool allNeg = true;

                for (int i = 0; i < 3; i++)
                {
                    bool pos = Values[face.vertices[i]] >= 0f;
                    positive[i] = pos;
                    if (pos) allNeg = false;
                    else allPos = false;
                }

                if (allNeg)
                {
                    NegativeFaces.Add(face);
                    continue;
                }

                if (allPos)
                {
                    PositiveFaces.Add(face);
                    continue;
                }

                int marchingTriangleIndex = 0;
                int triangleCornerIndex = -1;//corner that is alone
                int n = 1;
                for (int i = 0; i < 3; i++)
                {
                    if (positive[(i + 1) % 3] == positive[(i + 2) % 3])
                        triangleCornerIndex = i;

                    if (positive[i])
                        marchingTriangleIndex += n;

                    n *= 2;
                }

                var edges = MarchingTrianglesEdges[marchingTriangleIndex];

                //Creating or getting the "edge" vertices
                Vertex[] edgeVertices = new Vertex[2];
                Vector2[] uvs = new Vector2[2];
                Color[] colors = new Color[2];
                for (int i = 0; i < 2; i++)
                {
                    int c0 = edges[i];
                    int c1 = (edges[i] + 1) % 3;

                    Vertex corner0 = face.vertices[c0];
                    Vertex corner1 = face.vertices[c1];

                    ValueTuple<Vertex, Vertex> index0 = new(corner0, corner1);

                    float h0 = Values[corner0];
                    float h1 = Values[corner1];

                    float t;
                    if (h0 < h1)
                    {
                        t = -h0 / (h1 - h0);
                    }
                    else
                    {
                        t = 1 + h1 / (h0 - h1);
                    }
                    uvs[i] = Vector2.Lerp(face.GetUV(corner0), face.GetUV(corner1), t);
                    colors[i] = Color.Lerp(face.GetColor(corner0), face.GetColor(corner1), t);

                    if (!TriangeSideVertices.ContainsKey(index0))
                    {                       
                        Vertex newVertex = new Vertex(Vector3.Lerp(corner0.position, corner1.position, t));
                        ValueTuple<Vertex, Vertex> index1 = new(corner1, corner0);

                        TriangeSideVertices[index0] = newVertex;
                        TriangeSideVertices[index1] = newVertex;

                        NewVertices.Add(newVertex);
                    }

                    edgeVertices[i] = TriangeSideVertices[index0];
                }


                //Triangle face
                Vertex Corner = face.vertices[triangleCornerIndex];
                Face triangleFace = AddFace(Corner, edgeVertices[0], edgeVertices[1]);

                triangleFace.SetUV(edgeVertices[0], uvs[0]);
                triangleFace.SetUV(edgeVertices[1], uvs[1]);
                triangleFace.SetUV(Corner, face.GetUV(Corner));

                triangleFace.SetColor(edgeVertices[0], colors[0]);
                triangleFace.SetColor(edgeVertices[1], colors[1]);
                triangleFace.SetColor(Corner, face.GetColor(Corner));

                CopyAttributes(from: face, to: triangleFace);
                (positive[triangleCornerIndex] ? PositiveFaces : NegativeFaces).Add(triangleFace);

                //Smoothing edges
                if (smoothEdges)
                {
                    foreach (Edge edge in triangleFace.edges)
                    {
                        if (edge.Vertex0 != edgeVertices[0] && edge.Vertex0 != edgeVertices[1]) continue;
                        if (edge.Vertex1 != edgeVertices[0] && edge.Vertex1 != edgeVertices[1]) continue;
                        edge.smooth = true;
                        break;
                    }
                }

                //Quad face
                int i0 = (triangleCornerIndex + 1) % 3;
                int i1 = (triangleCornerIndex + 2) % 3;
                Corner = face.vertices[i0];
                Vertex Corner2 = face.vertices[i1];
                Face quadFace = AddFace(Corner, Corner2, edgeVertices[1], edgeVertices[0]);

                quadFace.SetUV(Corner, face.GetUV(Corner)); 
                quadFace.SetUV(Corner2, face.GetUV(Corner2)); 
                quadFace.SetUV(edgeVertices[0], uvs[0]);
                quadFace.SetUV(edgeVertices[1], uvs[1]);

                quadFace.SetColor(Corner, face.GetColor(Corner));
                quadFace.SetColor(Corner2, face.GetColor(Corner2));
                quadFace.SetColor(edgeVertices[0], colors[0]);
                quadFace.SetColor(edgeVertices[1], colors[1]);


                CopyAttributes(from: face, to: quadFace);
                (positive[i0] ? PositiveFaces : NegativeFaces).Add(quadFace);

                //Remove original face
                Remove(face);


            }

            void CopyAttributes(Face from, Face to)
            {
                foreach(var kv in from.Attributes)
                {
                    to.Attributes[kv.Key] = kv.Value;
                }
            }

            void RemoveSmallIslands()
            {
                HashSet<Vertex> IgnoredVertices = new HashSet<Vertex>();

                if (minIslandVertices <= 0) return;

                HashSet<Vertex> Vertices = new HashSet<Vertex>();
                Vertices.UnionWith(this.Vertices);

                HashSet<Vertex> Island = new HashSet<Vertex>();
                HashSet<Vertex> Border = new HashSet<Vertex>();

                HashSet<Vertex> neighbours = new HashSet<Vertex>();

                while (Vertices.Count > 0)
                {
                    Island.Clear();
                    Border.Clear();

                    var e = Vertices.GetEnumerator();
                    e.MoveNext();
                    Vertex vertex = e.Current;
                    bool positive = Values[vertex] >= 0f;

                    Border.Add(vertex);
                    Vertices.Remove(vertex);

                    
                    while (Border.Count > 0)
                    {
                        e = Border.GetEnumerator();
                        e.MoveNext();
                        Vertex borderVertex = e.Current;

                        Border.Remove(borderVertex);
                        Island.Add(borderVertex);

                        foreach(Vertex neighbour in borderVertex.GetNeighbours(result: neighbours))
                        {
                            if ((Values[neighbour] >= 0f) != positive) continue;
                            if(!Vertices.Contains(neighbour)) continue;
                            Vertices.Remove(neighbour);
                            Border.Add(neighbour);
                        }
                    }

                    if(Island.Count < minIslandVertices)
                    {
                        IgnoredVertices.UnionWith(Island);
                    }
                }

                foreach(Vertex v in IgnoredVertices)
                {
                    Values[v] = Values[v] >= 0 ? -smallValue : smallValue;
                }
            }
        }


    }
}
