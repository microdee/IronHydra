using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VVVV.PluginInterfaces.V2;
using Microsoft.Scripting.Hosting;
using System.Threading;

namespace VVVV.IronHydra
{
    public class PythonThread
    {
        protected Task _Task;
        protected CompiledPythonResource _Script;
        protected int _InvokationCount = 0;
        protected int _ErrorCount = 0;
        protected int _SuccessCount = 0;
        protected ScriptScope _Scope;
        protected Dictionary<string, object> _Variables = new Dictionary<string, object>();
        protected object _Result;
        protected Exception _Error;
        protected bool _Running = false;

        protected bool Initialized = false;
        public CancellationTokenSource CancelTokenSrc;
        public CancellationToken CancelToken;
        public bool BreakRequest = false;
        public bool RunRequest = false;

        public List<PythonThread> Neighbourhood;
        public int ThreadID = -1;

        public Task Task
        {
            get { return _Task; }
            set { }
        }
        public CompiledPythonResource Script
        {
            get { return _Script; }
            set { }
        }
        public int InvokationCount
        {
            get { return _InvokationCount; }
            set { }
        }
        public int SuccessCount
        {
            get { return _SuccessCount; }
            set { }
        }
        public int ErrorCount
        {
            get { return _ErrorCount; }
            set { }
        }
        public ScriptScope Scope
        {
            get { return _Scope; }
            set { }
        }
        public Dictionary<string, object> Variables
        {
            get { return _Variables; }
            set { }
        }
        public object Result
        {
            get { return _Result; }
            set { }
        }
        public Exception Error
        {
            get { return _Error; }
            set { }
        }
        public bool Running
        {
            get { return _Running; }
            set { }
        }

        public event EventHandler OnSuccess;
        public event EventHandler OnError;
        public event EventHandler OnInitialized;
        public event EventHandler OnTaskCompleted;

        public void Run(bool synced = false)
        {
            if (_Task != null)
                _Task.Dispose();

            _Running = true;
            _Task = new Task(() =>
            {
                try
                {
                    if (!Initialized)
                    {
                        _Scope = _Script.Engine.Engine.CreateScope();
                        _Scope.SetVariable("VVVVContext", this);
                        foreach (KeyValuePair<string, object> kvp in _Variables)
                        {
                            _Scope.SetVariable(kvp.Key, kvp.Value);
                        }
                        Initialized = true;
                    }
                    _Result = _Script.Compiled.Execute<object>(_Scope).ToString();
                    _SuccessCount++;
                }
                catch (Exception ex)
                {
                    _Error = ex;
                    _ErrorCount++;
                    if(OnError != null) OnError(this, EventArgs.Empty);
                }
            }, CancelToken);

            _Task.ContinueWith((Task task) =>
            {
                _Running = false;
                if (!task.IsCanceled)
                {
                    _InvokationCount++;
                    if (OnTaskCompleted != null) OnTaskCompleted(this, EventArgs.Empty);
                }
            });
            if (synced)
                _Task.RunSynchronously();
            else
                _Task.Start();
        }

        public void UpdateVariables()
        {
            foreach (KeyValuePair<string, object> kvp in _Variables)
            {
                _Scope.SetVariable(kvp.Key, kvp.Value);
            }
        }

        public PythonThread(CompiledPythonResource cpyr)
        {
            CancelTokenSrc = new CancellationTokenSource();
            CancelToken = CancelTokenSrc.Token;

            _Script = cpyr;
        }
    }
}
