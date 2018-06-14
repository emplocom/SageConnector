using System;
using Symfonia.Common.Defs;

namespace SageConnector.Logic
{
    public static class SageIdentifierUtils
    {
        public static string GetExternalIdForEmplo(this IIdentifier identifier)
        {
            switch (identifier.Kind)
            {
                case IdentifierKind.Local:
                    return identifier.IntId.ToString();
                case IdentifierKind.Global:
                    return identifier.GuidId.ToString();
                case IdentifierKind.Both:
                    return identifier.IntId.ToString();
                case IdentifierKind.None:
                default:
                    throw new Exception("No identifier kind!");
            }
        }
    }
}