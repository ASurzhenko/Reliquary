using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reliquary.Domain;
using UnityEngine;

namespace Reliquary.Tests.EditMode
{
    public class DomainBoundaryTests
    {
        [Serializable]
        private class AsmdefFlags
        {
            public string name;
            public bool noEngineReferences;
        }

        [Test]
        public void DomainAsmdef_StillDeclaresNoEngineReferences()
        {
            string path = Path.Combine(Application.dataPath, "Domain", "Reliquary.Domain.asmdef");
            Assert.That(File.Exists(path), Is.True, $"{path} is missing — the domain boundary is gone.");

            AsmdefFlags flags = JsonUtility.FromJson<AsmdefFlags>(File.ReadAllText(path));

            Assert.That(flags, Is.Not.Null);
            Assert.That(flags.name, Is.EqualTo("Reliquary.Domain"));
            Assert.That(flags.noEngineReferences, Is.True,
                "noEngineReferences was switched off — engine and UI types can now reach the domain.");
        }

        [Test]
        public void DomainAssembly_ReferencesNoUnityAssembly()
        {
            Assembly domain = typeof(RelicId).Assembly;

            Assert.That(domain.GetName().Name, Is.EqualTo("Reliquary.Domain"),
                "RelicId no longer compiles into the domain assembly — this test is inspecting something else.");

            string[] referenced = domain.GetReferencedAssemblies()
                .Select(assembly => assembly.Name)
                .ToArray();

            Assert.That(referenced.Any(name => name.StartsWith("UnityEngine") || name.StartsWith("UnityEditor")),
                Is.False, "domain references: " + string.Join(", ", referenced));
        }
    }
}
