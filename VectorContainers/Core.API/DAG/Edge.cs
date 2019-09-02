namespace Core.API.DAG
{
    public class Edge
    {
        public object Info { get; set; }
        public Vertex Vertex { get; set; }

        public Edge(object info, Vertex vertex)
        {
            Vertex = vertex;
            Info = info;
        }
    }
}
