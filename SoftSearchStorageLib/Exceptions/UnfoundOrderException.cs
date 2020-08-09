using System;
using System.Runtime.Serialization;

namespace SoftSearchStorageLib.Exceptions
{
    [Serializable]
    public class UnfoundOrderException : ApplicationException
    {
        protected UnfoundOrderException()
        {
        }

        protected UnfoundOrderException(string message) : base(message)
        {
        }

        protected UnfoundOrderException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnfoundOrderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public static UnfoundOrderException OrderIsUnfound(string orderId)
        {
            return new UnfoundOrderException($"Order '{orderId}' is unfound");
        }
    }
}
