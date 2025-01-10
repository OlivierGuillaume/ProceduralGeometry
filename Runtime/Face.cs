using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace OG.ProceduralGeometry
{
    public class Face
    {
        internal readonly List<Vertex> vertices = new();
        internal readonly List<Edge> edges = new();

        internal readonly Dictionary<ValueTuple<Vertex, int>, Vector2> uvs = new();
    
        public readonly Dictionary<int, float> Attributes = new();

        public int submesh = 0;

        public void SetUV(Vector2 uv, int channel = 0)
        {
            foreach (var vertex in vertices)
                uvs[new(vertex, channel)] = uv;
        }

        public void SetUV(Vertex vertex, Vector2 uv, int channel = 0)
        {
            uvs[new(vertex, channel)] = uv;
        }

        public Vector2 GetUV(Vertex vertex, int channel = 0)
        {
            ValueTuple<Vertex, int> key = new(vertex, channel);

            Vector2 uv;

            if(!uvs.TryGetValue(key, out uv))  return Vector2.zero;

            return uv;
        }

        public enum TriangulationMode { Fan, Radial }

        public TriangulationMode triangulation = TriangulationMode.Fan;

        public ReadOnlyCollection<Vertex> Vertices => vertices.AsReadOnly();
        public ReadOnlyCollection<Edge> Edges => edges.AsReadOnly();

        public int VerticesCount => vertices.Count;

        public Vector3 Centroid
        {
            get
            {
                Vector3 center = Vector3.zero;

                foreach(Vertex v in vertices)
                {
                    center += v.position;
                }

                return center / vertices.Count;
            }
        }

        /// <summary>
        /// Normal vector with amplitude twice the area of the face
        /// </summary>
        private Vector3 GetRawNormal()
        {
            if (vertices.Count == 3)
                return Vector3.Cross(vertices[1].position - vertices[0].position, vertices[2].position - vertices[0].position);

            Vector3 centroid = Centroid;
            Vector3 normal = Vector3.zero;

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 p0 = vertices[i].position;
                Vector3 p1 = vertices[(i + 1) % vertices.Count].position;

                normal += Vector3.Cross(p0 - centroid, p1 - centroid);
            }

            return normal;
        }

        public Vector3 Normal => GetRawNormal().normalized;

        public float Area => 0.5f * GetRawNormal().magnitude;

        public Face(params Vertex[] vertices)
        {
            SetVertices(vertices);
        }

        public void SetVertices(params Vertex[] vertices)
        {
            ClearReferences();
            this.vertices.Clear();
            this.edges.Clear();

            for (int i = 0; i < vertices.Length; i++)
            {
                Vertex v0 = vertices[i];
                this.vertices.Add(v0);

                Vertex v1 = vertices[(i + 1) % vertices.Length];
                Edge edge = v0.GetEdgeWith(v1);
                if (edge == null)
                {
                    edge = new Edge(v0, v1, smooth: false);
                }

                edges.Add(edge);
            }
            SetReferences();
        }

        public HashSet<Face> GetNeighbours()
        {
            HashSet<Face> result = new HashSet<Face>();
            foreach(Edge edge in edges)
            {
                foreach(Face face in edge.faces)
                {
                    if (face == this) continue;
                    result.Add(face);
                }
            }
            return result;
        }

        public void CopyAttributesFrom(Face other)
        {
            foreach(var kv in other.Attributes)
                Attributes[kv.Key] = kv.Value;
        }
        
        internal List<Vertex> GetTriangles()
        {
            switch (triangulation)
            {
                case TriangulationMode.Fan:
                    return GetTrianglesFan();
                case TriangulationMode.Radial:
                    return GetTrianglesRadial();
            }

            return new List<Vertex>();
        }

        private List<Vertex> GetTrianglesFan()
        {
            List<Vertex> tris = new List<Vertex>();

            int fanOrigin = 0;

            int n = vertices.Count;

            if (n == 4)//quad
            {
                float d0sqr = (vertices[0].position - vertices[2].position).sqrMagnitude;
                float d1sqr = (vertices[1].position - vertices[3].position).sqrMagnitude;

                if(d0sqr > d1sqr) fanOrigin = 1;//use the smallest diagonal
            }


            for (int i = 1; i < vertices.Count - 1; i++)
            {
                tris.Add(vertices[fanOrigin]);
                tris.Add(vertices[(fanOrigin + i) % n]);
                tris.Add(vertices[(fanOrigin + i + 1) % n]);
            }

            return tris;
        }

        private List<Vertex> GetTrianglesRadial()
        {
            List<Vertex> tris = new List<Vertex>();

            Vector3 centroidPos = Vector3.zero;
            Vector2 centroidUV = Vector3.zero;

            foreach (Vertex v in vertices)
            {
                centroidPos += v.position;
                centroidUV += GetUV(v);
            }

            centroidPos /= vertices.Count;
            centroidUV /= vertices.Count;

            Vertex centroid = new Vertex(centroidPos);
            SetUV(centroid, centroidUV);

            for (int i = 0; i < vertices.Count ; i++)
            {
                tris.Add(centroid);
                tris.Add(vertices[i]);
                tris.Add(vertices[(i + 1) % vertices.Count]);
            }

            return tris;
        }


        /// <summary>
        /// Call this before changing vertices and edges or before deleting the face.
        /// </summary>
        internal void ClearReferences()
        {
            foreach (Vertex v in vertices) v.faces.Remove(this);
            foreach (Edge edge in edges) edge.faces.Remove(this);
        }

        /// <summary>
        /// Call this after changing vertices and edges.
        /// </summary>
        internal void SetReferences()
        {
            foreach (Vertex v in vertices) v.faces.Add(this);
            foreach (Edge edge in edges) edge.faces.Add(this);
        }

        ~Face()
        {
            ClearReferences();
        }
    }
}
