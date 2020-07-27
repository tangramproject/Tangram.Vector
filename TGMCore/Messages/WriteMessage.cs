// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMCore.Messages
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
