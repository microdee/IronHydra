using System;
using System.Collections.Generic;
using VVVV.PluginInterfaces.V2;

namespace VVVV.IronHydra
{
    public abstract class VVVVEventHelper
    {
        public int ID;
        public Dictionary<string, ISpread<bool>> StatusBools = new Dictionary<string, ISpread<bool>>();
        public ISpread<string> ErrorMessage;
    }

    public class CompilationEvents : VVVVEventHelper
    {
        public void OnCompiled(object o, EventArgs e)
        {
            StatusBools["Working"][ID] = false;
            StatusBools["Success"][ID] = true;
        }
        public void OnError(object o, EventArgs e)
        {
            StatusBools["Working"][ID] = false;
            StatusBools["Error"][ID] = true;
            var cprwc = o as CompiledPythonResourceWithCode;
            ErrorMessage[ID] = cprwc.CompilationError.Message + "\n\n" + cprwc.CompilationError.StackTrace;
        }

        public CompilationEvents() { }
    }
    public class TaskEvents : VVVVEventHelper
    {
        public ISpread<object> Result;

        public void OnSuccess(object o, EventArgs e)
        {
            var pt = o as PythonThread;
            Result[ID] = pt.Result;
            StatusBools["Success"][ID] = true;
        }
        public void OnError(object o, EventArgs e)
        {
            StatusBools["Error"][ID] = true;
            var pt = o as PythonThread;
            ErrorMessage[ID] = pt.Error.Message;
        }
        public void OnTaskCompleted(object o, EventArgs e)
        {
            StatusBools["Working"][ID] = false;
        }

        public TaskEvents() { }
    }
}
