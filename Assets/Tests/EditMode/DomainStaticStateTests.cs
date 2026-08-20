using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Reliquary.Domain;

namespace Reliquary.Tests.EditMode
{
    public class DomainStaticStateTests
    {
        [Test]
        public void DomainHoldsNoMutableStaticState()
        {
            foreach (Type type in typeof(RelicId).Assembly.GetTypes())
            {
                if (Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute)))
                {
                    // Delegate caches the compiler emits for lambdas. Not state anybody wrote, and not
                    // something a rule can read: excluding them narrows what this test proves.
                    continue;
                }

                foreach (FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.Public
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    Assert.That(field.IsInitOnly || field.IsLiteral, Is.True,
                        $"{type.Name}.{field.Name} is mutable static state. " +
                        "Compiler-generated types are excluded, so this is code someone wrote.");
                }
            }
        }
    }
}
