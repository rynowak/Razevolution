using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace Razevolution.VSTools
{
    public class OutputWindowTrace : Trace
    {
        private readonly IVsOutputWindowPane _pane;

        public OutputWindowTrace(IVsOutputWindowPane pane)
        {
            _pane = pane;
        }

        public override void WriteLine(string message)
        {
            _pane.OutputStringThreadSafe(message + Environment.NewLine);
        }
    }
}
