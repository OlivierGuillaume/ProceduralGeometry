using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace OG.ProceduralGeometry
{
    public class Vertex
    {
        public Vector3 position;


        internal readonly List<Face> faces = new List<Face>();
        internal readonly List<Edge> edges = new List<Edge>();

        public Vertex(Vector3 position)
        {
            this.position = position;
        }

        public Vertex(float x, float y, float z)
        {
            this.position = new(x,y,z);
        }

        public ReadOnlyCollection<Face> Faces => faces.AsReadOnly();

        public HashSet<Vertex> GetNeighbours(HashSet<Vertex> result = null)
        {
            if(result == null) result = new HashSet<Vertex>();
            else result.Clear();

            foreach(Edge e in edges)
            {
                result.Add(e.Vertex0);
                result.Add(e.Vertex1);
            }
            result.Remove(this);
            return result;
        }
        /// <summary>
        /// Set the uvs of this vertex for all the faces
        /// </summary>
        public void SetUV(Vector2 uv, int channel = 0)
        {
            foreach (var face in faces)
            {
                face.SetUV(this, uv, channel);
            }
        }


        /// <returns>The edge connecting this vertex with the other. Null if it doesn't exist.</returns>
        internal Edge GetEdgeWith(Vertex other)
        {
            foreach(Edge edge in edges)
            {
                if(edge.Vertex0 == other) return edge;
                if(edge.Vertex1 == other) return edge;
            }
            return null;
        }
    }
}
