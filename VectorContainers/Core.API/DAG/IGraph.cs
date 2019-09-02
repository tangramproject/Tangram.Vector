using System.Collections.Generic;

namespace Core.API.DAG
{
    public interface IGraph
    {
        List<Vertex> VerticesList { get; }

        bool AddEdge(object edgeInfo, Vertex tailVertex, Vertex headVertex);
        bool AddEdge(object edgeInfo, string tailVertexKey, string headVertexKey);

        void AddVertex(string key, object info);

        bool AreAdjacent(Vertex tailVertex, Vertex headVertex);
        bool AreAdjacent(string tailVertexKey, string headVertexKey);
        int Degree(Vertex vertex);
        int Degree(string key);
        bool DeleteEdge(Vertex tailVertex, Vertex headVertex);
        bool DeleteVertex(Vertex vertex);
        bool DeleteVertex(string key);
        bool DeleteVertexWithEdges(Vertex vertex);
        bool DeleteVertexWithEdges(string key);
        List<Vertex> GetAdjacentsVertexs(Vertex vertex);
        List<Vertex> GetAdjacentsVertexs(string key);
        List<Edge> GetInAdjacentsEdges(Vertex vertex);
        List<Vertex> GetInAdjacentsVertexs(Vertex vertex);
        List<Vertex> GetInAdjacentsVertexs(string key);
        Vertex GetVertex(string key);
        int InDegree(Vertex vertex);
        int InDegree(string key);
        bool IsEmpty();
        int OutDegree(Vertex vertex);
        int OutDegree(string key);
    }
}