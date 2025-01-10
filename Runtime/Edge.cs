using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OG.ProceduralGeometry
{
    public class Edge
    {
        public bool smooth = false;

        readonly Vertex[] vertices = new Vertex[2];
        internal readonly List<Face> faces = new List<Face>();

        public ReadOnlyCollection<Face> Faces => faces.AsReadOnly();

        public Vertex Vertex0 => vertices[0]; 
        public Vertex Vertex1 => vertices[1]; 

        internal Edge(Vertex v0, Vertex v1, bool smooth)
        {
            vertices[0] = v0;
            vertices[1] = v1;
            v0.edges.Add(this);
            v1.edges.Add(this);
            this.smooth = smooth;
        }

        ~Edge()
        {
            vertices[0].edges.Remove(this);
            vertices[1].edges.Remove(this);
        }
    }
}
