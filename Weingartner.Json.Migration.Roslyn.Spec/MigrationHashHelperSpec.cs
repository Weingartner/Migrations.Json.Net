using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Weingartner.Json.Migration.Roslyn.Spec
{
    public class MigrationHashHelperSpec
    {
        [Fact]
        public void ShouldGetAllDataMembers()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"using System.Runtime.Serialization;

public class A
{
    public string PropertyA { get; }
    [DataMember] public string PropertyB { get; }
    public int FieldA { get; }
    [DataMember] public int FieldB { get; }
}");

            var compilation = CSharpCompilation.Create(
                "MyCompilation",
                syntaxTrees: new[] { tree },
                references: new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(DataMemberAttribute).Assembly.Location)
                });
            var model = compilation.GetSemanticModel(tree);

            var typeDecl = tree
                .GetRoot()
                .ChildNodes()
                .OfType<TypeDeclarationSyntax>()
                .Single();

            var dataMemberAttributeType = compilation.GetTypeByMetadataName(Constants.DataMemberAttributeMetadataName);

            var dataMembers = MigrationHashHelper.GetDataMembers(typeDecl, CancellationToken.None, model, dataMemberAttributeType);

            dataMembers.Count.Should().Be(2);
            dataMembers[0].Identifier.Should().Be("FieldB");
            dataMembers[0].Type.Should().Be("int");
            dataMembers[1].Identifier.Should().Be("PropertyB");
            dataMembers[1].Type.Should().Be("string");
        }
    }
}
