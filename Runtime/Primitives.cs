using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OG.ProceduralGeometry
{
    public partial class ProceduralGeometry
    {
        public static ProceduralGeometry Quad(float size = 1f)
        {
            ProceduralGeometry mesh = new ProceduralGeometry();
            float hs = size * 0.5f;

            Vertex v0 = new(hs, 0, hs);
            Vertex v1 = new(hs, 0, -hs);
            Vertex v2 = new(-hs, 0, -hs);
            Vertex v3 = new(-hs, 0, hs);

            mesh.AddFace(new Face(v0, v1, v2, v3));

            return mesh;
        }

        public static ProceduralGeometry Cube(float size = 1f)
        {
            ProceduralGeometry mesh = new ProceduralGeometry();
            float hs = size * 0.5f;

            Vertex v0 = new(hs, hs, hs);
            Vertex v1 = new(hs, hs, -hs);
            Vertex v2 = new(-hs, hs, -hs);
            Vertex v3 = new(-hs, hs, hs);
            Vertex v4 = new(hs, -hs, hs);
            Vertex v5 = new(hs, -hs, -hs);
            Vertex v6 = new(-hs, -hs, -hs);
            Vertex v7 = new(-hs, -hs, hs);

            mesh.AddFace(v0, v1, v2, v3);//top
            mesh.AddFace(v7, v6, v5, v4);//bottom
            mesh.AddFace(v4, v5, v1, v0);//left
            mesh.AddFace(v5, v6, v2, v1);//back
            mesh.AddFace(v6, v7, v3, v2);//right
            mesh.AddFace(v7, v4, v0, v3);//front

            return mesh;
        }

        public static ProceduralGeometry SquareGrid(int width, int height, float cellSize = 1)
        {
            Vertex[,] Vertices = new Vertex[width+1, height+1];

            for(int x = 0; x < width + 1; x++)
            {
                for(int y = 0; y < height + 1; y++)
                {
                    Vertices[x, y] = new(x * cellSize, 0f, y * cellSize);
                }
            }

            ProceduralGeometry mesh = new ProceduralGeometry();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vertex v0 = Vertices[x, y];
                    Vertex v1 = Vertices[x, y+1];
                    Vertex v2 = Vertices[x + 1, y + 1];
                    Vertex v3 = Vertices[x + 1, y];

                    mesh.AddFace(v0,v1, v2, v3);
                }
            }

            return mesh;
        }

        public static ProceduralGeometry TriangleGrid(int width, int height, float sideSize = 1)
        {
            Vertex[,] Vertices = new Vertex[width + 1, height + 1];

            float h = sideSize * Mathf.Sqrt(3) / 2f;
            Vector3 offset = -0.5f * new Vector3(width * sideSize, 0f, height * h);

            for (int x = 0; x < width + 1; x++)
            {
                for (int y = 0; y < height + 1; y++)
                {
                    Vector3 pos = new Vector3((x + (y % 2) * 0.5f) * sideSize, 0f, y * h) + offset;
                    Vertices[x, y] = new(pos);
                }
            }

            ProceduralGeometry mesh = new ProceduralGeometry();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vertex v0 = Vertices[x, y];
                    Vertex v1 = Vertices[x, y + 1];
                    Vertex v2 = Vertices[x + 1, y + 1];
                    Vertex v3 = Vertices[x + 1, y];

                    if(y % 2 == 0)
                    {
                        mesh.AddFace(v0, v1, v3);
                        mesh.AddFace(v1, v2, v3);
                    }
                    else
                    {
                        mesh.AddFace(v0, v1, v2);
                        mesh.AddFace(v0, v2, v3);
                    }
                    
                }
            }

            return mesh;
        }


        public static ProceduralGeometry Cylinder(float radius, float height, int sides)
        {
            if(sides < 3)
            {
                throw new ArgumentException("Cylinder can't have less than 3 sides");
            }

            Vertex[] TopVertices = new Vertex[sides];
            Vertex[] BottomVertices = new Vertex[sides];

            for(int i =  0; i < sides; i++)
            {
                float angle = i * Mathf.PI * 2f / sides;

                float x = radius * Mathf.Cos(angle);
                float z = radius * Mathf.Sin(angle);

                TopVertices[i] = new Vertex(x, height * 0.5f, z);
                BottomVertices[i] = new Vertex(x, -height * 0.5f, z);
            }

            ProceduralGeometry mesh = new ProceduralGeometry();

            HashSet<Face> sideFaces = new HashSet<Face>();

            for (int i = 0; i < sides; i++)
            {
                Vertex v0 = TopVertices[i];
                Vertex v1 = TopVertices[(i + 1) % sides];
                Vertex v2 = BottomVertices[(i + 1) % sides];
                Vertex v3 = BottomVertices[i];

                sideFaces.Add(mesh.AddFace(v0, v1, v2, v3));
            }

            mesh.AddFace(TopVertices.Reverse().ToArray()).triangulation = Face.TriangulationMode.Radial;
            mesh.AddFace(BottomVertices).triangulation = Face.TriangulationMode.Radial;

            foreach(Face face in sideFaces)
            {
                foreach(Edge edge in face.Edges)
                {
                    bool isSideEdge = true;
                    foreach(Face otherFace in edge.faces)
                    {
                        if (!sideFaces.Contains(otherFace))
                        {
                            isSideEdge = false;
                            break;
                        }
                    }
                    edge.smooth = isSideEdge;
                }
            }

            return mesh;
        }

        public static ProceduralGeometry CubeSphere(float radius, int resolution)
        {
            if (resolution < 1)
            {
                throw new ArgumentException("Resolution should be at least 1");
            }

            if(resolution == 1)
            {
                return Cube(radius / Mathf.Sqrt(2));
            }


            Vector3 center = .5f * Vector3.one;

            Dictionary<Vector3Int, Vertex> Vertices = new Dictionary<Vector3Int, Vertex>();

            ProceduralGeometry geom = new ProceduralGeometry();

            for(int face = 0; face < 6; face++)
            {
                bool front = face % 2 == 0;
                Vector3Int originIndex = front ? Vector3Int.zero : Vector3Int.one * resolution;

                Vector3Int xAxis = Vector3Int.zero;
                xAxis[face / 2] = front ? 1 : -1;

                Vector3Int yAxis = Vector3Int.zero;
                yAxis[(face / 2 + 1)%3] = front ? 1 : -1;

                if (front) (xAxis, yAxis) = (yAxis, xAxis);

                for (int xFace = 0; xFace <= resolution; xFace++)
                {
                    for(int yFace = 0; yFace <= resolution; yFace++)
                    {

                        Vector3Int index0 = originIndex + xFace * xAxis + yFace * yAxis;

                        Vertex v0 = GetVertex(index0);

                        if(xFace == resolution || yFace == resolution) continue;

                        Vertex v1 = GetVertex(index0 + xAxis);
                        Vertex v2 = GetVertex(index0 + xAxis + yAxis);
                        Vertex v3 = GetVertex(index0 + yAxis);

                        geom.AddFace(v0,v1,v2,v3);
                    }
                }
            }

            foreach (Face face in geom.Faces)
                foreach (Edge edge in face.Edges)
                    edge.smooth = true;

            return geom;

            Vertex GetVertex(Vector3Int index)
            {

                Vertex v;
                if (!Vertices.TryGetValue(index, out v))
                {

                    Vector3 pos = (Vector3)index / (float)resolution - center;
                    pos.Normalize();
                    v = new Vertex(pos * radius);
                    Vertices.Add(index, v);
                }

                return v;
            }
        }

        public static ProceduralGeometry UVSphere(float radius, int rings, int segments)
        {
            if (rings < 2 || segments < 3)
            {
                throw new ArgumentException("Rings must be at least 2 and segments must be at least 3.");
            }

            ProceduralGeometry geom = new ProceduralGeometry();
            Dictionary<Vector2Int, Vertex> vertices = new();

            for (int ring = 0; ring < rings; ring++)
            {
                for (int segment = 0; segment < segments; segment++)
                {
                    Vertex v0 = GetVertex(ring, segment);
                    Vertex v1 = GetVertex(ring, segment + 1);
                    Vertex v2 = GetVertex(ring + 1, segment + 1);
                    Vertex v3 = GetVertex(ring + 1, segment);

                    if (v0 == v1)
                        geom.AddFace(v0, v2, v3);//North pole
                    else if (v2 == v3)
                        geom.AddFace(v0, v1, v2);//South pole
                    else geom.AddFace(v0, v1, v2, v3);
                }
            }

            foreach (Face face in geom.Faces)
                foreach (Edge edge in face.Edges)
                    edge.smooth = true;

            return geom;


            Vertex GetVertex(int ring, int segment)
            {
                Vector2Int index = GetIndex(ring, segment);

                Vertex v;

                if(!vertices.TryGetValue(index, out v))
                {
                    v = new(GetVertexPosition(ring, segment));
                    vertices.Add(index, v);
                }

                return v;

                Vector2Int GetIndex(int ring, int segment)
                {
                    if (ring == 0) return new(0, 0);
                    if(ring == rings) return new(rings, 0);

                    if(segment == segments) segment = 0;

                    return new Vector2Int(ring, segment);
                }
            }

            Vector3 GetVertexPosition(int ring, int segment)
            {
                float v = (float)ring / rings;
                float theta = v * Mathf.PI;
                float u = (float)segment / segments;
                float phi = u * 2 * Mathf.PI;

                return new Vector3(
                    Mathf.Sin(theta) * Mathf.Cos(phi),
                    Mathf.Cos(theta),
                    Mathf.Sin(theta) * Mathf.Sin(phi)
                ) * radius;
            }
        }

    }
}
