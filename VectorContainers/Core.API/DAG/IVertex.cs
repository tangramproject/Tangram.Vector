using System.Collections.Generic;

namespace Core.API.DAG
{
    public interface IVertex
    {
        object Info { get; set; }
        string Key { get; set; }
        List<Edge> EdgeList { get; set; }

        void DeleteAllEdges();
        bool DeleteEdge(Vertex vertex);
        bool InsertEdge(object edgeInfo, Vertex vertexPtr);
        bool IsAdjacent(Vertex vertex);
    }
}