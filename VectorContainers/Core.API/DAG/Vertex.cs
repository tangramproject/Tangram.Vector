using System.Collections.Generic;

namespace Core.API.DAG
{
    public class Vertex : IVertex
    {
        public object Info { get; set; }
        public string Key { get; set; }
        public List<Edge> EdgeList { get; set; }

        public Vertex(string key, object info)
        {
            Info = info;
            Key = key;
            EdgeList = new List<Edge>();
        }

        /// <summary>
        /// Ises the adjacent.
        /// </summary>
        /// <returns><c>true</c>, if adjacent was ised, <c>false</c> otherwise.</returns>
        /// <param name="vertex">Vertex.</param>
        public bool IsAdjacent(Vertex vertex)
        {
            foreach (var found in EdgeList)
            {
                if (found.Vertex != null)
                {
                    if (found.Vertex.Equals(vertex))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Inserts the edge.
        /// </summary>
        /// <returns><c>true</c>, if edge was inserted, <c>false</c> otherwise.</returns>
        /// <param name="edgeInfo">Edge info.</param>
        /// <param name="vertexPtr">Vertex ptr.</param>
        public bool InsertEdge(object edgeInfo, Vertex vertexPtr)
        {

            if (!IsAdjacent(vertexPtr))
            {
                var edge = new Edge(edgeInfo, vertexPtr);
                EdgeList.Add(edge);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Deletes the edge.
        /// </summary>
        /// <returns><c>true</c>, if edge was deleted, <c>false</c> otherwise.</returns>
        /// <param name="vertex">Vertex.</param>
        public bool DeleteEdge(Vertex vertex)
        {
            foreach (Edge theEdge in EdgeList)
            {
                if (theEdge.Vertex.Equals(vertex))
                {
                    EdgeList.Remove(theEdge);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Deletes all edges.
        /// </summary>
        public void DeleteAllEdges()
        {
            EdgeList.Clear();
        }
    }
}
