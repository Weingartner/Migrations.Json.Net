using Xunit;
using Weingartner.Json.Migration.Roslyn;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Weingartner.Json.Migration.Roslyn.Spec.Helpers;

namespace Weingartner.Json.Migration.Roslyn.Spec
{
    public class AddMigrationMethodCodeFixProviderSpec : CodeFixVerifier
    {
        
        [Fact]
        public void ShouldAddMigrationMethod()
        {
            var smallFailingDoc = @"using Weingartner.Json.Migration;
using System.Runtime.Serialization;

namespace NameSpaceName
{
    [Migratable(""1234"")]
    [DataContract]
    class TypeName
    {
        [DataMember]
        public int A { get; set; }
        [DataMember]
        public int B { get; set; }
    }
}";
            var expected = @"using Weingartner.Json.Migration;
using System.Runtime.Serialization;

namespace NameSpaceName
{
    [Migratable(""327430167"")]
    [DataContract]
    class TypeName
    {
        [DataMember]
        public int A { get; set; }
        [DataMember]
        public int B { get; set; }

        private static JToken Migrate_1(JToken data, JsonSerializer serializer)
        {
            throw new System.NotImplementedException();
            return data;
        }
    }
}";

            VerifyCSharpFix( smallFailingDoc, expected, null, true );
        }

        [Fact]
        public void ShouldAddSecondMigrationMethod()
        {
            var smallFailingDoc = @"using Weingartner.Json.Migration;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NameSpaceName
{
    [Migratable(""327430167"")]
    [DataContract]
    class TypeName
    {
        [DataMember]
        public int A { get; set; }
        [DataMember]
        public int B { get; set; }
        [DataMember]
        public int C { get; set; }

        private static JToken Migrate_1(JToken data, JsonSerializer serializer)
        {
            data[""B""] = 0;
            return data;
        }
    }
}";
            var expected = @"using Weingartner.Json.Migration;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NameSpaceName
{
    [Migratable(""-1225206030"")]
    [DataContract]
    class TypeName
    {
        [DataMember]
        public int A { get; set; }
        [DataMember]
        public int B { get; set; }
        [DataMember]
        public int C { get; set; }

        private static JToken Migrate_1(JToken data, JsonSerializer serializer)
        {
            data[""B""] = 0;
            return data;
        }

        private static JToken Migrate_2(JToken data, JsonSerializer serializer)
        {
            throw new System.NotImplementedException();
            return data;
        }
    }
}";

            VerifyCSharpFix(smallFailingDoc, expected, null, true);
        }

        [Fact]
        public void ShouldAddMigrationMethodToStruct()
        {
            var smallFailingDoc = @"using Weingartner.Json.Migration;
using System.Runtime.Serialization;

namespace NameSpaceName
{
    [Migratable(""1234"")]
    [DataContract]
    struct TypeName
    {
        [DataMember]
        public int A { get; set; }
        [DataMember]
        public int B { get; set; }
    }
}";
            var expected = @"using Weingartner.Json.Migration;
using System.Runtime.Serialization;

namespace NameSpaceName
{
    [Migratable(""327430167"")]
    [DataContract]
    struct TypeName
    {
        [DataMember]
        public int A { get; set; }
        [DataMember]
        public int B { get; set; }

        private static JToken Migrate_1(JToken data, JsonSerializer serializer)
        {
            throw new System.NotImplementedException();
            return data;
        }
    }
}";

            VerifyCSharpFix(smallFailingDoc, expected, null, true);
        }

        [Fact]
        public void ShouldNotFailIfAMethodHasArrayReturnType()
        {
            var smallFailingDoc = @"using Weingartner.Json.Migration;
using System.Runtime.Serialization;

namespace NameSpaceName
{
    [Migratable(""1234"")]
    [DataContract]
    class TypeName
    {
        [DataMember]
        public int A { get; set; }
        [DataMember]
        public int B { get; set; }

        public int[] SomeMethod()
        {
            return new[] { 1, 2, 3 };
        }
    }
}";
            var expected = @"using Weingartner.Json.Migration;
using System.Runtime.Serialization;

namespace NameSpaceName
{
    [Migratable(""327430167"")]
    [DataContract]
    class TypeName
    {
        [DataMember]
        public int A { get; set; }
        [DataMember]
        public int B { get; set; }

        public int[] SomeMethod()
        {
            return new[] { 1, 2, 3 };
        }

        private static JToken Migrate_1(JToken data, JsonSerializer serializer)
        {
            throw new System.NotImplementedException();
            return data;
        }
    }
}";

            VerifyCSharpFix(smallFailingDoc, expected, null, true);
        }

        [Fact]
        public void ShouldNotFailIfAMethodHasArrayParameters()
        {
            var smallFailingDoc = @"using Weingartner.Json.Migration;
using System.Runtime.Serialization;

namespace NameSpaceName
{
    [Migratable(""1234"")]
    [DataContract]
    class TypeName
    {
        [DataMember]
        public int A { get; set; }
        [DataMember]
        public int B { get; set; }

        public int SomeMethod(double[] para)
        {
            return 0;
        }
    }
}";
            var expected = @"using Weingartner.Json.Migration;
using System.Runtime.Serialization;

namespace NameSpaceName
{
    [Migratable(""327430167"")]
    [DataContract]
    class TypeName
    {
        [DataMember]
        public int A { get; set; }
        [DataMember]
        public int B { get; set; }

        public int SomeMethod(double[] para)
        {
            return 0;
        }

        private static JToken Migrate_1(JToken data, JsonSerializer serializer)
        {
            throw new System.NotImplementedException();
            return data;
        }
    }
}";

            VerifyCSharpFix(smallFailingDoc, expected, null, true);
        }

        [Fact]
        public void ShouldNotFailWithArrays()
        {
            var smallFailingDoc = @"using Weingartner.Json.Migration;
using System.Runtime.Serialization;

namespace NameSpaceName
{
    [Migratable(""1234"")]
    [DataContract]
    class TypeName
    {
        [DataMember]
        public int A { get; set; }
        [DataMember]
        public int B { get; set; }

        public int[] SomeMethod(double[] para)
        {
            return new[] { 1, 2, 3 };
        }
    }
}";
            var expected = @"using Weingartner.Json.Migration;
using System.Runtime.Serialization;

namespace NameSpaceName
{
    [Migratable(""327430167"")]
    [DataContract]
    class TypeName
    {
        [DataMember]
        public int A { get; set; }
        [DataMember]
        public int B { get; set; }

        public int[] SomeMethod(double[] para)
        {
            return new[] { 1, 2, 3 };
        }

        private static JToken Migrate_1(JToken data, JsonSerializer serializer)
        {
            throw new System.NotImplementedException();
            return data;
        }
    }
}";

            VerifyCSharpFix(smallFailingDoc, expected, null, true);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new AddMigrationMethodCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MigrationHashAnalyzer();
        }
    }
}