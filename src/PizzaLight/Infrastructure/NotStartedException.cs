using System;
using System.Runtime.Serialization;

namespace PizzaLight.Infrastructure
{
    [Serializable]
    internal class NotStartedException : Exception
    {
        public NotStartedException()
        {
        }

        public NotStartedException(string message) : base(message)
        {
        }

        public NotStartedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NotStartedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}