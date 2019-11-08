using System;
namespace Core.API.Messages
{
    public class WriteMessage
    {
        public WriteMessage(string content)
        {
            Content = content;
        }

        public string Content { get; private set; }
    }
}
