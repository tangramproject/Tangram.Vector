using System.Collections.Generic;
using System.Linq;

namespace Core.API.DAG
{
    public class Graph : IGraph
    {
        public List<Vertex> VerticesList { get; }

        public Graph()
        {
            VerticesList = new List<Vertex>();
        }

        /// <summary>
        /// Checks if the vertices list is empty.
        /// </summary>
        /// <returns><c>true</c>, if empty, <c>false</c> otherwise.</returns>
        public bool IsEmpty()
        {
            return VerticesList.Count == 0;
        }

        /// <summary>
        /// Gets the vertex.
        /// </summary>
        /// <returns>The vertex.</returns>
        /// <param name="key">Key.</param>
        public Vertex GetVertex(string key)
        {
            for (int i = 0; i < VerticesList.Count; i++)
            {
                var vertex = VerticesList[i];
                if (vertex.Key == key)
                    return vertex;
            }

            return null;
        }

        /// <summary>
        /// Adds the vertex.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="info">Info.</param>
        public void AddVertex(string key, object info)
        {
            var vert = new Vertex(key, info);
            VerticesList.Add(vert);
        }

        /// <summary>
        /// Adds the edge.
        /// </summary>
        /// <returns><c>true</c>, if edge was added, <c>false</c> otherwise.</returns>
        /// <param name="edgeInfo">Edge info.</param>
        /// <param name="tailVertex">Tail vertex.</param>
        /// <param name="headVertex">Head vertex.</param>
        public bool AddEdge(object edgeInfo, Vertex tailVertex, Vertex headVertex)
        {
            return tailVertex.InsertEdge(edgeInfo, headVertex);
        }

        /// <summary>
        /// Adds the edge.
        /// </summary>
        /// <returns><c>true</c>, if edge was added, <c>false</c> otherwise.</returns>
        /// <param name="edgeInfo">Edge info.</param>
        /// <param name="tailVertexKey">Tail vertex key.</param>
        /// <param name="headVertexKey">Head vertex key.</param>
        public bool AddEdge(object edgeInfo, string tailVertexKey, string headVertexKey)
        {
            return GetVertex(tailVertexKey).InsertEdge(edgeInfo, GetVertex(headVertexKey));
        }

        /// <summary>
        /// Ares the adjacent.
        /// </summary>
        /// <returns><c>true</c>, if adjacent was ared, <c>false</c> otherwise.</returns>
        /// <param name="tailVertex">Tail vertex.</param>
        /// <param name="headVertex">Head vertex.</param>
        public bool AreAdjacent(Vertex tailVertex, Vertex headVertex)
        {
            return tailVertex.IsAdjacent(headVertex);
        }

        /// <summary>
        /// Ares the adjacent.
        /// </summary>
        /// <returns><c>true</c>, if adjacent was ared, <c>false</c> otherwise.</returns>
        /// <param name="tailVertexKey">Tail vertex key.</param>
        /// <param name="headVertexKey">Head vertex key.</param>
        public bool AreAdjacent(string tailVertexKey, string headVertexKey)
        {
            return GetVertex(tailVertexKey).IsAdjacent(GetVertex(headVertexKey));
        }

        /// <summary>
        /// Gets the adjacents vertexs.
        /// </summary>
        /// <returns>The adjacents vertexs.</returns>
        /// <param name="vertex">Vertex.</param>
        public List<Vertex> GetAdjacentsVertexs(Vertex vertex)
        {
            var adjList = new List<Vertex>();
            for (int i = 0; i < vertex.EdgeList.Count; i++)
            {
                var adjEdge = vertex.EdgeList[i];
                adjList.Add(adjEdge.Vertex);
            }

            return adjList;
        }

        /// <summary>
        /// Gets the adjacents vertexs.
        /// </summary>
        /// <returns>The adjacents vertexs.</returns>
        /// <param name="key">Key.</param>
        public List<Vertex> GetAdjacentsVertexs(string key)
        {
            var adjList = new List<Vertex>();
            for (int i = 0; i < GetVertex(key).EdgeList.Count; i++)
            {
                var adjEdge = GetVertex(key).EdgeList[i];
                adjList.Add(adjEdge.Vertex);
            }

            return adjList;
        }

        /// <summary>
        /// Gets the in adjacents vertexs.
        /// </summary>
        /// <returns>The in adjacents vertexs.</returns>
        /// <param name="vertex">Vertex.</param>
        public List<Vertex> GetInAdjacentsVertexs(Vertex vertex)
        {
            var adjList = new List<Vertex>();
            for (int i = 0; i < VerticesList.Count; i++)
            {
                var v = VerticesList[i];
                if (v.IsAdjacent(vertex))
                    adjList.Add(v);
            }

            return adjList;
        }

        /// <summary>
        /// Gets the in adjacents edges.
        /// </summary>
        /// <returns>The in adjacents edges.</returns>
        /// <param name="vertex">Vertex.</param>
        public List<Edge> GetInAdjacentsEdges(Vertex vertex)
        {
            var adjList = new List<Edge>();
            for (int i = 0; i < VerticesList.Count; i++)
            {
                var v = VerticesList[i];
                if (v.IsAdjacent(vertex))
                    adjList.Add(v.EdgeList.FirstOrDefault(e => e.Vertex.Key == v.Key));
            }

            return adjList;
        }

        /// <summary>
        /// Gets the in adjacents vertexs.
        /// </summary>
        /// <returns>The in adjacents vertexs.</returns>
        /// <param name="key">Key.</param>
        public List<Vertex> GetInAdjacentsVertexs(string key)
        {
            var adjList = new List<Vertex>();
            for (int i = 0; i < VerticesList.Count; i++)
            {
                var v = VerticesList[i];
                if (v.IsAdjacent(GetVertex(key)))
                    adjList.Add(v);
            }

            return adjList;
        }

        /// <summary>
        /// Outs the degree.
        /// </summary>
        /// <returns>The degree.</returns>
        /// <param name="vertex">Vertex.</param>
        public int OutDegree(Vertex vertex)
        {
            return vertex.EdgeList.Count;
        }

        /// <summary>
        /// Outs the degree.
        /// </summary>
        /// <returns>The degree.</returns>
        /// <param name="key">Key.</param>
        public int OutDegree(string key)
        {
            return GetVertex(key).EdgeList.Count;
        }

        /// <summary>
        /// Ins the degree.
        /// </summary>
        /// <returns>The degree.</returns>
        /// <param name="vertex">Vertex.</param>
        public int InDegree(Vertex vertex)
        {
            var inDegree = 0;
            for (int i = 0; i < VerticesList.Count; i++)
            {
                var v = VerticesList[i];
                if (v.IsAdjacent(vertex))
                    inDegree++;
            }

            return inDegree;
        }

        /// <summary>
        /// Ins the degree.
        /// </summary>
        /// <returns>The degree.</returns>
        /// <param name="key">Key.</param>
        public int InDegree(string key)
        {
            var inDegree = 0;
            for (int i = 0; i < VerticesList.Count; i++)
            {
                var v = VerticesList[i];
                if (v.IsAdjacent(GetVertex(key)))
                    inDegree++;
            }

            return inDegree;
        }

        /// <summary>
        /// Degree the specified vertex.
        /// </summary>
        /// <returns>The degree.</returns>
        /// <param name="vertex">Vertex.</param>
        public int Degree(Vertex vertex)
        {
            return InDegree(vertex) + OutDegree(vertex);
        }

        /// <summary>
        /// Degree the specified key.
        /// </summary>
        /// <returns>The degree.</returns>
        /// <param name="key">Key.</param>
        public int Degree(string key)
        {
            return InDegree(key) + OutDegree(key);
        }

        /// <summary>
        /// Deletes the edge.
        /// </summary>
        /// <returns><c>true</c>, if edge was deleted, <c>false</c> otherwise.</returns>
        /// <param name="tailVertex">Tail vertex.</param>
        /// <param name="headVertex">Head vertex.</param>
        public bool DeleteEdge(Vertex tailVertex, Vertex headVertex)
        {
            return tailVertex.DeleteEdge(headVertex);
        }

        /// <summary>
        /// Deletes the vertex.
        /// </summary>
        /// <returns><c>true</c>, if vertex was deleted, <c>false</c> otherwise.</returns>
        /// <param name="vertex">Vertex.</param>
        public bool DeleteVertex(Vertex vertex)
        {
            for (int i = 0; i < VerticesList.Count; i++)
            {
                var v = VerticesList[i];
                if (v.Equals(vertex))
                {
                    vertex.DeleteAllEdges();
                    VerticesList.Remove(vertex);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Deletes the vertex.
        /// </summary>
        /// <returns><c>true</c>, if vertex was deleted, <c>false</c> otherwise.</returns>
        /// <param name="key">Key.</param>
        public bool DeleteVertex(string key)
        {
            for (int i = 0; i < VerticesList.Count; i++)
            {
                var v = VerticesList[i];
                if (v.Key == key)
                {
                    var vertex = GetVertex(key);
                    vertex.DeleteAllEdges();
                    VerticesList.Remove(vertex);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Deletes the vertex with edges.
        /// </summary>
        /// <returns><c>true</c>, if vertex with edges was deleted, <c>false</c> otherwise.</returns>
        /// <param name="vertex">Vertex.</param>
        public bool DeleteVertexWithEdges(Vertex vertex)
        {
            for (int i = 0; i < VerticesList.Count; i++)
            {
                var v = VerticesList[i];
                if (v.IsAdjacent(vertex))
                    v.DeleteEdge(vertex);
            }

            return true;
        }

        /// <summary>
        /// Deletes the vertex with edges.
        /// </summary>
        /// <returns><c>true</c>, if vertex with edges was deleted, <c>false</c> otherwise.</returns>
        /// <param name="key">Key.</param>
        public bool DeleteVertexWithEdges(string key)
        {
            var vertex = GetVertex(key);
            for (int i = 0; i < VerticesList.Count; i++)
            {
                var v = VerticesList[i];
                if (v.IsAdjacent(vertex))
                    v.DeleteEdge(vertex);
            }

            return true;
        }

    }
}
