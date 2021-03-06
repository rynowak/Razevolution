﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Razor.Directives;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.FileProviders;
using Razevolution.Tooling.Messages;

namespace Razevolution.Tooling
{
    public class DefaultMessageProcessor : MessageProcessor
    {
        public DefaultMessageProcessor(MessageQueue queue) 
            : base(queue)
        {
            State = new State();
        }

        protected State State { get; }

        protected override void ProcessMessage(Message message)
        {
            if (message is SolutionMessage)
            {
                Visit((SolutionMessage)message);
            }
            else if (message is MetadataMessage)
            {
                Visit((MetadataMessage)message);
            }
            else if (message is ErrorMessage)
            {
                Visit((UnknownMessage)message);
            }
            else if (message is UnknownMessage)
            {
                Visit((UnknownMessage)message);
            }
            else
            {
                Console.WriteLine($"unknown message: {message.ToString()}");
            }
        }

        protected virtual void Visit(SolutionMessage message)
        {
            Console.WriteLine("clearing projects");
            State.Projects.Clear();

            if (message.Projects != null)
            {
                foreach (var project in message.Projects)
                {
                    Console.WriteLine($"adding project {project.Name}");

                    var p = new State.Project()
                    {
                        Id = project.Id,
                        Name = project.Name,
                        Path = project.Path,
                        Root = Path.GetDirectoryName(project.Path),
                    };

                    if (project.References != null)
                    {
                        foreach (var reference in project.References)
                        {
                            p.References.Add(reference, null);
                        }
                    }

                    var documents = Directory.EnumerateFiles(p.Root, "*.cshtml", SearchOption.AllDirectories);
                    p.Documents.AddRange(documents);

                    State.Projects.Add(project.Id, p);
                }
            }
        }

        protected virtual void Visit(MetadataMessage message)
        {
            State.Project project;
            if (!State.Projects.TryGetValue(message.Id, out project))
            {
                Console.WriteLine($"project {message.ProjectName} not found");
            }

            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"updating metadata for {project.Name}");
            project.Metadata = message.Bytes;

            var references = new List<MetadataReference>();

            if (project.Metadata != null)
            {
                references.Add(MetadataReference.CreateFromImage(ImmutableArray.Create(project.Metadata)));
            }

            // Lazy compute of MetadataReference
            {
                var referencesUpdated = new List<KeyValuePair<string, MetadataReference>>();
                foreach (var reference in project.References)
                {
                    var metadata = reference.Value;
                    if (metadata == null)
                    {
                        referencesUpdated.Add(new KeyValuePair<string, MetadataReference>(reference.Key, MetadataReference.CreateFromFile(reference.Key)));
                    }
                }

                foreach (var update in referencesUpdated)
                {
                    project.References[update.Key] = update.Value;
                }
            }

            references.AddRange(project.References.Values);

            var compilation = CSharpCompilation.Create(
                Path.GetFileNameWithoutExtension(Path.GetTempFileName()), 
                references: references, 
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var chunkTree = new DefaultChunkTreeCache(new PhysicalFileProvider(Path.GetDirectoryName(project.Path)));
            var host = new MvcRazorHost(chunkTree, new SymbolTableTagHelperDescriptorProvider(compilation))
            {
                DesignTimeMode = true,
            };
            var engine = new RazorTemplateEngine(host);

            var syntaxTrees = new List<SyntaxTree>();

            foreach (var document in project.Documents)
            {
                var relativePath = document.Substring(project.Root.Length).Replace('\\', '/');

                Console.WriteLine($"generating {document}");

                GeneratorResults result;
                using (var stream = File.OpenRead(document))
                {
                    result = host.GenerateCode(relativePath, stream);
                    if (!result.Success)
                    {
                        foreach (var error in result.ParserErrors)
                        {
                            Console.WriteLine($"parser error {error.ToString()}");
                        }

                        continue;
                    }
                }

                Console.WriteLine($"generated {document}");
                //Console.WriteLine(result.GeneratedCode);
                Console.WriteLine();
                Console.WriteLine();

                syntaxTrees.Add(CSharpSyntaxTree.ParseText(result.GeneratedCode, path: document));
            }

            Console.WriteLine($"generated documents in {stopwatch.Elapsed.TotalMilliseconds}ms");

            compilation = compilation.AddSyntaxTrees(syntaxTrees);

            var errors = compilation.GetDiagnostics();
            foreach (var error in errors.Where(e => e.Severity == DiagnosticSeverity.Error))
            {
                Console.WriteLine($"roslyn error: {error.ToString()}");
            }

            Console.WriteLine($"updated documents in {stopwatch.Elapsed.TotalMilliseconds}ms");
        }

        protected virtual void Visit(ErrorMessage message)
        {
            Console.WriteLine($"error message: {message.Exception} - {message.OriginalText}");
        }

        protected virtual void Visit(UnknownMessage message)
        {
            Console.WriteLine($"unknown message: {(message).Body}");
        }
    }
}
