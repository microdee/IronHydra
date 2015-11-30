using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace VVVV.IronHydra
{
    public class PythonEngine
    {
        protected ScriptEngine _Engine;
        public ScriptEngine Engine
        {
            get { return _Engine; }
            set { }
        }

        public PythonEngine()
        {
            _Engine = Python.CreateEngine();
            if (Directory.Exists(@"C:\Python27\"))
            {
                ICollection<string> paths = _Engine.GetSearchPaths();
                paths.Add(@"C:\Python27\Lib\site-packages\");
                paths.Add(@"C:\Python27\Lib\");
                _Engine.SetSearchPaths(paths);
            }
        }
    }
    public class CompiledPythonResource
    {
        protected CompiledCode _Compiled;
        public CompiledCode Compiled
        {
            get { return _Compiled; }
            set { }
        }

        protected PythonEngine _Engine;
        public PythonEngine Engine
        {
            get { return _Engine; }
            set { }
        }

        protected CompiledPythonResource() { }
    }
    public class CompiledPythonResourceWithCode : CompiledPythonResource
    {
        public Task CompilationTask;
        private ScriptSource _Source;
        private Exception _CompilationError;

        public event EventHandler OnSourceCreated;
        public event EventHandler OnCompiled;
        public event EventHandler OnCompilationError;

        public ScriptSource Source
        {
            get { return _Source; }
            set { }
        }
        public Exception CompilationError
        {
            get { return _CompilationError; }
            set { }
        }

        public CompiledPythonResourceWithCode(string source, SourceCodeKind kind)
        {
            CompilationTask = new Task(() => {
                _CompilationError = null;
                try
                {
                    _Engine = new PythonEngine();
                    _Source = _Engine.Engine.CreateScriptSourceFromString(source, kind);
                    //OnSourceCreated(this, EventArgs.Empty);
                    _Compiled = _Source.Compile();
                    //OnCompiled(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    _CompilationError = ex;
                    OnCompilationError(this, EventArgs.Empty);
                }
            });
            CompilationTask.ContinueWith((Task task) =>
            {
                if (_CompilationError == null)
                    OnCompiled(this, EventArgs.Empty);
            });
        }
    }
}
