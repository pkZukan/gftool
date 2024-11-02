
using System.Diagnostics.CodeAnalysis;

namespace GFTool.Renderer
{
    public enum MessageType
    {
        LOG,
        WARNING,
        ERROR
    };

    public struct Message
    {
        public MessageType Type;
        public string Content;
        public Message(MessageType type, string content)
        {
            Type = type;
            Content = content;
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return Content.Equals(obj);
        }

        public override int GetHashCode() 
        { 
            return Content.GetHashCode(); 
        }
    }
    public class MessageHandler
    {
        private static readonly Lazy<MessageHandler> lazy = new Lazy<MessageHandler>(() => new MessageHandler());
        public static MessageHandler Instance { get { return lazy.Value; } }

        public event EventHandler<Message> MessageCallback;

        private MessageHandler()
        { 
            //
        }

        public void AddMessage(MessageType type, string content)
        {
            MessageCallback?.Invoke(this, new Message(type, content));
        }

    }
}
