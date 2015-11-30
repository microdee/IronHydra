using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace VVVV.IronHydra
{
    public class PythonThreadGroup
    {
        protected Task _Task;
        protected List<PythonThread> _Children = new List<PythonThread>();
        protected bool _Running = false;

        public bool RunRequest = false;
        public bool BreakRequest = false;
        public CancellationTokenSource CancelTokenSrc;
        public CancellationToken CancelToken;

        public Task Task
        {
            get { return _Task; }
            set { }
        }
        public bool Running
        {
            get { return _Running; }
            set { }
        }
        public List<PythonThread> Children
        {
            get { return _Children; }
            set { }
        }

        public event EventHandler OnTaskCompleted;

        public void Run(bool synced = false)
        {
            if (_Task != null)
            {
                if(_Task.IsCompleted || _Task.IsCanceled)
                    _Task.Dispose();
                /*else
                {
                    CancelTokenSrc.Cancel();
                    _Task.Dispose();
                }*/
            }

            _Running = true;
            _Task = new Task(() =>
            {
                foreach(PythonThread pt in _Children)
                {
                    if (pt != null)
                    {
                        if (pt.BreakRequest)
                        {
                            pt.RunRequest = false;
                            pt.BreakRequest = false;
                        }
                        if (pt.RunRequest)
                        {
                            pt.BreakRequest = false;
                            pt.Run(synced = true);
                        }
                    }
                }
            }, CancelToken);

            _Task.ContinueWith((Task task) =>
            {
                _Running = false;

                bool Killme = true;
                foreach (PythonThread pt in _Children)
                {
                    if (pt != null)
                    {
                        if (pt.RunRequest) Killme = false;
                    }
                }
                if (Killme) BreakRequest = true;

                if (OnTaskCompleted != null) OnTaskCompleted(this, EventArgs.Empty);
            });
            if (synced)
                _Task.RunSynchronously();
            else
                _Task.Start();
        }

        public PythonThreadGroup()
        {
            CancelTokenSrc = new CancellationTokenSource();
            CancelToken = CancelTokenSrc.Token;
        }
    }
}
