using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OG.ProceduralGeometry
{
    public partial class ProceduralGeometry
    {
        /// <summary>
        /// Set the edges betweens faces of the surface set smooth or sharp.
        /// </summary>
        public void SetSmooth(HashSet<Face> surface, bool smooth = true)
        {
            foreach (Face face in surface)
            {
                foreach (Edge edge in face.Edges)
                {
                    bool inside = true;

                    foreach (Face adjFace in edge.faces)
                    {
                        if (!surface.Contains(adjFace))
                        {
                            inside = false;
                            break;
                        }
                    }

                    if(inside)
                        edge.smooth = smooth;
                }
            }
        }

        public void SetSmooth() => SetSmooth(Faces, true);
        public void SetSharp() => SetSmooth(Faces, false);
        public void AutoSmooth(float angle = 30f)
        {
            HashSet<Edge> edges = new HashSet<Edge>();

            foreach (Face face in Faces)
            {
                foreach (Edge edge in face.Edges)
                {
                    edges.Add(edge);
                }
            }

            foreach(Edge edge in edges)
            {
                if (edge.faces.Count <= 1) continue;

                edge.smooth = Vector3.Angle(edge.faces[0].Normal, edge.faces[1].Normal) < angle;
            }
        }
    }
}
