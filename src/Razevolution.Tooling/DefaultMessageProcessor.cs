using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
                        Documents = Directory.EnumerateFiles(Path.GetDirectoryName(project.Path), "*.cshtml", SearchOption.AllDirectories).ToList(),
                        Id = project.Id,
                        Name = project.Name,
                        Path = project.Path,
                        References = new List<string>(project.References ?? Enumerable.Empty<string>()),
                        Root = Path.GetDirectoryName(project.Path),
                    };

                    foreach (var document in p.Documents)
                    {
                        Console.WriteLine($"\t found document {document}");
                    }

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

            Console.WriteLine($"updating metadata for {project.Name}");
            project.Metadata = message.Bytes;

            var references = new List<MetadataReference>();

            if (project.Metadata != null)
            {
                references.Add(MetadataReference.CreateFromImage(ImmutableArray.Create(project.Metadata)));
            }

            foreach (var reference in project.References)
            {
                references.Add(MetadataReference.CreateFromFile(reference));
            }

            var compilation = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(Path.GetTempFileName()), references: references, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var chunkTree = new DefaultChunkTreeCache(new PhysicalFileProvider(Path.GetDirectoryName(project.Path)));
            var host = new MvcRazorHost(chunkTree, new SymbolTableTagHelperDescriptorProvider(compilation));
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
                Console.WriteLine(result.GeneratedCode);
                Console.WriteLine();
                Console.WriteLine();

                syntaxTrees.Add(CSharpSyntaxTree.ParseText(result.GeneratedCode, path: document));
            }

            compilation = compilation.AddSyntaxTrees(syntaxTrees);

            var errors = compilation.GetDiagnostics();
            foreach (var error in errors.Where(e => e.Severity == DiagnosticSeverity.Error))
            {
                Console.WriteLine($"roslyn error: {error.ToString()}");
            }

            Console.WriteLine("updated documents");
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
