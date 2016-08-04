//------------------------------------------------------------------------------
// <copyright file="VSPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Razevolution.VSTools
{
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(VSPackage.PackageGuidString)]
    public sealed class VSPackage : Package
    {
        public const string PackageGuidString = "9c65816c-52eb-489b-ac36-f0cfa7fa9fde";

        private const string OutputWindowGuidString = "843E6397-D6EA-47C2-A45F-DB8FB862E802";

        private ProjectServerListener _listener;
        private IVsOutputWindow _outputWindow;
        private IVsOutputWindowPane _pane;
        private ProjectServer _server;
        private Trace _trace;
        private VisualStudioWorkspace _workspace;

        #region Package Members

        protected override void Initialize()
        {
            base.Initialize();

            _outputWindow = (IVsOutputWindow)GetService(typeof(SVsOutputWindow));

            var guid = new Guid(OutputWindowGuidString);
            ErrorHandler.ThrowOnFailure(_outputWindow.CreatePane(ref guid, "Razor Tools", 1, 0));

            ErrorHandler.ThrowOnFailure(_outputWindow.GetPane(ref guid, out _pane));

            _trace = new OutputWindowTrace(_pane);

            var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            _workspace = componentModel.GetService<VisualStudioWorkspace>();

            _workspace.WorkspaceChanged += Workspace_WorkspaceChanged;

            _server = new ProjectServer(_workspace, _trace);
            _server.Start();

            var client = new Process();
            client.StartInfo.FileName = "dotnet";
            client.StartInfo.Arguments = $"run -p \"D:\\prototype3\\Razevolution\\src\\Razevolution.Tooling\" -- -p {_server.Port}";
            client.Start();
        }

        private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            _pane.OutputStringThreadSafe($"Event: {e.Kind} - {e.ProjectId} - {e.DocumentId}" + Environment.NewLine);
        }

        #endregion
    }
}
