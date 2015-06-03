using System;
using System.Runtime.Serialization;

namespace Weingartner.Json.Migration
{
    [Serializable]
    public class DataVersionTooHighException : MigrationException
    {
        public DataVersionTooHighException()
        {
        }

        public DataVersionTooHighException(string message) : base(message)
        {
        }

        public DataVersionTooHighException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DataVersionTooHighException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}