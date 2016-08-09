using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Razevolution.Tooling.Messages;

namespace Razevolution.VSTools
{
    public class ProjectServer
    {
        private static readonly int Period = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;
        private static readonly EmitOptions EmitOptions = new EmitOptions(metadataOnly: true, tolerateErrors: true);

        private readonly ProjectServerListener _listener;
        private readonly object _lock;
        private readonly Trace _trace;
        private readonly Workspace _workspace;

        private bool _solutionDirty;
        private bool _metadataDirty;

        private Timer _timer;

        public ProjectServer(Workspace workspace, Trace trace)
        {
            _workspace = workspace;
            _trace = trace;

            _listener = new ProjectServerListener(trace, OnConnected);
            _lock = new object();
            _timer = new Timer(OnTimerTick, state: null, dueTime: -1, period: Period);
        }

        public int Port => _listener.Port;

        public void Start()
        {
            lock (_lock)
            {
                _listener.Start();

                _workspace.WorkspaceChanged += OnWorkspaceChanged;
                _timer.Change(Period, Period);
            }
        }

        private void OnConnected(ProjectServerClient client)
        {
            client.Send(VersionMessage.MessageType, new VersionMessage() { Version = 1 });

            var solution = _workspace.CurrentSolution;
            client.Send(SolutionMessage.MessageType, CreateSolutionMessage(solution));
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            lock (_lock)
            {
                switch (e.Kind)
                {
                    case WorkspaceChangeKind.SolutionAdded:
                    case WorkspaceChangeKind.SolutionChanged:
                    case WorkspaceChangeKind.SolutionCleared:
                    case WorkspaceChangeKind.SolutionReloaded:
                    case WorkspaceChangeKind.SolutionRemoved:
                    case WorkspaceChangeKind.ProjectAdded:
                    case WorkspaceChangeKind.ProjectChanged:
                    case WorkspaceChangeKind.ProjectReloaded:
                    case WorkspaceChangeKind.ProjectRemoved:
                        _solutionDirty = true;
                        _metadataDirty = true;
                        break;

                    default:
                        _metadataDirty = true;
                        break;
                }
            }
        }

        private void OnTimerTick(object state)
        {
            lock (_lock)
            {
                var solution = _workspace.CurrentSolution;

                if (_solutionDirty)
                {
                    _solutionDirty = false;

                    _trace.WriteLine("updating solution");

                    _listener.Broadcast(SolutionMessage.MessageType, CreateSolutionMessage(solution));
                }

                if (_metadataDirty)
                {
                    _metadataDirty = false;

                    _trace.WriteLine("updating metadata");

                    var batch = new List<Task<CompilationResult>>();

                    foreach (var project in solution.Projects)
                    {
                        _trace.WriteLine($"updating {project.Name}");
                        batch.Add(BeginCompile(project));
                    }

                    Task.WhenAll(batch).ContinueWith(OnCompilationComplete);
                }
            }
        }

        private async Task<CompilationResult> BeginCompile(Project project)
        {
            Compilation compilation;
            if (project.TryGetCompilation(out compilation))
            {
                _trace.WriteLine($"{project.Name} already has a compilation");
                return new CompilationResult(project, compilation);
            }

            _trace.WriteLine("compiling {project.Name}");
            compilation = await project.GetCompilationAsync();

            return new CompilationResult(project, compilation);
        }

        private void OnCompilationComplete(Task<CompilationResult[]> task)
        {
            var results = task.Result;
            var stream = new MemoryStream();
            for (var i = 0; i < results.Length; i++)
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.SetLength(0);

                var project = results[i].Project;
                var compilation = results[i].Compilation;

                var emit = compilation.Emit(stream, options: EmitOptions);

                _trace.WriteLine($"emit {project.Name} - {(emit.Success ? "success" : "failed")}");
                if (emit.Success)
                {
                    _listener.Broadcast(MetadataMessage.MessageType, new MetadataMessage()
                    {
                        AssemblyName = compilation.AssemblyName,
                        Bytes = stream.ToArray(),
                        Id = project.Id.Id,
                        ProjectName = project.Name,
                    });
                }
            }
        }

        private SolutionMessage CreateSolutionMessage(Solution solution)
        {
            var message = new SolutionMessage()
            {
                Projects = new List<SolutionMessage.Project>(),
            };

            foreach (var project in solution.Projects)
            {
                message.Projects.Add(new SolutionMessage.Project()
                {
                    Id = project.Id.Id,
                    Name = project.Name,
                    Path = project.FilePath,
                    References = project.MetadataReferences.OfType<PortableExecutableReference>().Select(r => r.FilePath).ToList(),
                });
            }

            return message;
        }

        private class CompilationResult
        {
            public CompilationResult(Project project, Compilation compilation)
            {
                Compilation = compilation;
                Project = project;
            }

            public Compilation Compilation { get; }

            public Project Project { get; }
        }
    }
}
