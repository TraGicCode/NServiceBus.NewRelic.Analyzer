
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Analyze
{
    public static class SemanticExtensions
    {
       

        public static INamespaceSymbol Namespace(this SemanticModel model, string name) =>
            model.LookupNamespacesAndTypes(0, name: name).OfType<INamespaceSymbol>().FirstOrDefault();

        public static INamespaceSymbol Namespace(this INamespaceSymbol symbol, string name) =>
            symbol.GetNamespaceMembers().FirstOrDefault(member => member.Name == name);

    }
}