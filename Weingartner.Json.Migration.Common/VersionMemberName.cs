using System;

namespace Weingartner.Json.Migration.Common
{
    public interface IVersionMemberName
    {
        string VersionPropertyName { get; }
        string VersionBackingFieldName { get; }
    }

    public static class VersionMemberName
    {
        private static IVersionMemberName _instance;

        public static IVersionMemberName Instance
        {
            get { return _instance ?? (_instance = new InvalidCsVersionMemberName()); }
        }

        public static bool TrySetInstance(IVersionMemberName instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            if (_instance != null) return false;

            _instance = instance;
            return true;
        }
    }

    public class InvalidCsVersionMemberName : IVersionMemberName
    {
        public string VersionPropertyName { get { return "<>Version"; } }
        public string VersionBackingFieldName { get { return "<>_version"; } }
    }

    public class ValidCsVersionMemberName : IVersionMemberName
    {
        public string VersionPropertyName { get { return "Version"; } }
        public string VersionBackingFieldName { get { return "_version"; } }
    }
}
