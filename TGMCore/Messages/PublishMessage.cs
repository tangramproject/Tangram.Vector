// TGMCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMCore.Messages
{
    public class PublishMessage
    {
        public string Topic { get; set; }
        public object Payload { get; set; }

        public PublishMessage()
        { }

        public PublishMessage(string topic, object payload)
        {
            Topic = topic;
            Payload = payload;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static PublishMessage Empty()
        {
            return new PublishMessage();
        }
    }
}
