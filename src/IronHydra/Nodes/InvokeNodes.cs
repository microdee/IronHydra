using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

namespace VVVV.IronHydra.Nodes
{
    public enum TaskIterationMoment
    {
        Immediately,
        Never
    };

    [PluginInfo(
        Name = "Iterate",
        Category = "Python",
        Author = "microdee"
    )]
    public class PythonInvokeAbstractNode : IPluginEvaluate
    {
        [Input("Input")]
        public Pin<CompiledPythonResource> FInput;

        [Input("Variables")]
        public ISpread<ISpread<string>> FVars;
        [Input("Values")]
        public ISpread<ISpread<object>> FVals;
        [Input("Update Variables")]
        public ISpread<bool> FUpdateVars;

        [Input("Start", IsBang = true)]
        public ISpread<bool> FStart;
        [Input("Kill", IsBang = true)]
        public ISpread<bool> FKill;
        [Input("Break", IsBang = true)]
        public ISpread<bool> FBreak;
        [Input("Continue", IsSingle = true)]
        public ISpread<TaskIterationMoment> FContinue;
        [Input("Real Threads", DefaultValue = 2)]
        public ISpread<int> FRealThreads;

        [Output("Output")]
        public ISpread<object> FResult;
        [Output("Last Error Message")]
        public ISpread<string> FErrorMessage;

        [Output("Neighbourhood")]
        public ISpread<List<PythonThread>> FNeighbourhood;
        [Output("Thread Groups")]
        public ISpread<List<PythonThreadGroup>> FThreadGroups;
        [Output("Python Thread")]
        public ISpread<PythonThread> FTask;
        [Output("Total Invokation Count")]
        public ISpread<int> FInvokeCount;
        [Output("Success Count")]
        public ISpread<int> FSuccessCount;
        [Output("Error Count")]
        public ISpread<int> FErrorCount;
        [Output("Running")]
        public ISpread<bool> FRunning;

        public List<PythonThread> Tasks = new List<PythonThread>();
        public List<PythonThreadGroup> Threads = new List<PythonThreadGroup>();

        [Import]
        public IHDEHost HDEHost;
        [Import]
        public IPluginHost PluginHost;
        [Import]
        public IPluginHost2 PluginHost2;

        private int CurrentThreadGroup(int i)
        {
            return (int)Math.Floor((float)i / (FInput.SliceCount / Math.Max(1, FRealThreads[0])));
        }

        public void ScheduleForRunning(int i)
        {
            Tasks[i] = new PythonThread(FInput[i]);
            Tasks[i].Neighbourhood = Tasks;
            Tasks[i].ThreadID = i;
            Tasks[i].RunRequest = true;

            for (int j = 0; j < FVars[i].SliceCount; j++)
            {
                Tasks[i].Variables.Add(FVars[i][j], FVals[i][j]);
            }
        }

        public void Evaluate(int SpreadMax)
        {
            if (FInput.IsConnected)
            {
                FResult.SliceCount = FInput.SliceCount;
                FErrorMessage.SliceCount = FInput.SliceCount;
                FInvokeCount.SliceCount = FInput.SliceCount;
                FSuccessCount.SliceCount = FInput.SliceCount;
                FErrorCount.SliceCount = FInput.SliceCount;
                FRunning.SliceCount = FInput.SliceCount;

                if(Tasks.Count != FInput.SliceCount)
                {
                    int diff = Math.Abs(FInput.SliceCount - Tasks.Count);
                    if (Tasks.Count > FInput.SliceCount)
                        Tasks.RemoveRange(Tasks.Count - diff, diff);
                    if (Tasks.Count < FInput.SliceCount)
                    {
                        for(int i=0; i< diff; i++)
                        {
                            Tasks.Add(null);
                        }
                    }
                }

                int RealThreadCount = FRealThreads[0];

                if (Threads.Count != RealThreadCount)
                {
                    int diff = Math.Abs(RealThreadCount - Threads.Count);
                    if (Threads.Count > RealThreadCount)
                        Threads.RemoveRange(Threads.Count - diff, diff);
                    if (Threads.Count < RealThreadCount)
                    {
                        for (int i = 0; i < diff; i++)
                        {
                            Threads.Add(new PythonThreadGroup());
                        }
                    }
                }

                FNeighbourhood[0] = Tasks;
                FThreadGroups[0] = Threads;

                bool StartAny = false;
                for (int i = 0; i < FStart.SliceCount; i++)
                {
                    if (FStart[i])
                    {
                        for(int j=0; j<Threads.Count; j++)
                        {
                            Threads[j].Children.Clear();
                            StartAny = true;
                        }
                        break;
                    }
                }
                for (int i = 0; i < FInput.SliceCount; i++)
                {
                    int tgi = CurrentThreadGroup(i);
                    if (FStart[i])
                    {
                        if (Tasks[i] == null)
                        {
                            ScheduleForRunning(i);
                            Threads[tgi].RunRequest = true;
                        }
                        else if (Tasks[i].Script.GetHashCode() != FInput[i].GetHashCode())
                        {
                            Tasks[i].Task.Dispose();
                            ScheduleForRunning(i);
                            Threads[tgi].RunRequest = true;
                        }
                        else if (Tasks[i].Task.IsCompleted)
                        {
                            Tasks[i].RunRequest = true;
                            Tasks[i].BreakRequest = false;
                            Threads[tgi].RunRequest = true;
                        }
                    }
                    if (FKill[i])
                    {
                        if (Tasks[i] != null)
                        {
                            if (!Tasks[i].Task.IsCompleted)
                            {
                                Tasks[i].CancelTokenSrc.Cancel();
                            }
                        }
                    }
                }
                if (StartAny)
                {
                    for (int i = 0; i < FInput.SliceCount; i++)
                    {
                        int tgi = CurrentThreadGroup(i);
                        Threads[tgi].Children.Add(Tasks[i]);
                    }

                    foreach(PythonThreadGroup thread in Threads)
                    {
                        if(thread.RunRequest)
                        { 
                            /*
                            if (thread.Task != null)
                            {
                                if (thread.Task.IsCompleted)
                                {
                                    thread.Task.Dispose();
                                }
                                else
                                {
                                    thread.CancelTokenSrc.Cancel();
                                    thread.Task.Dispose();
                                }
                            }
                            */
                            thread.OnTaskCompleted += (object o, EventArgs e) =>
                            {
                                var ptg = o as PythonThreadGroup;
                                if (ptg.BreakRequest) ptg.BreakRequest = false;
                                else
                                {
                                    if (FContinue[0] == TaskIterationMoment.Immediately)
                                    {
                                        ptg.Run();
                                    }
                                }
                            };
                            thread.Run();
                        }
                    }
                }

                for (int i = 0; i < FInput.SliceCount; i++)
                {
                    if (Tasks[i] != null)
                    {
                        if (Tasks[i].Error != null) FErrorMessage[i] = Tasks[i].Error.Message + "\n\n" + Tasks[i].Error.StackTrace;
                        else FErrorMessage[i] = "none";

                        FInvokeCount[i] = Tasks[i].InvokationCount;
                        FSuccessCount[i] = Tasks[i].SuccessCount;
                        FErrorCount[i] = Tasks[i].ErrorCount;
                        FResult[i] = Tasks[i].Result;
                        FRunning[i] = Tasks[i].Running;

                        if (FUpdateVars[i])
                        {
                            for (int j = 0; j < FVars[i].SliceCount; j++)
                            {
                                if (Tasks[i].Variables.ContainsKey(FVars[i][j]))
                                    Tasks[i].Variables[FVars[i][j]] = FVals[i][j];
                                else
                                    Tasks[i].Variables.Add(FVars[i][j], FVals[i][j]);
                            }
                            Tasks[i].UpdateVariables();
                        }
                        FTask[i] = Tasks[i];

                        if (FBreak[i])
                        {
                            Tasks[i].BreakRequest = true;
                        }
                    }
                    else
                    {
                        FInvokeCount[i] = -1;
                        FSuccessCount[i] = -1;
                        FErrorCount[i] = -1;
                    }
                }
            }
            else
            {
                FResult.SliceCount = 0;
                Tasks.Clear();
                FErrorMessage.SliceCount = 0;
                FInvokeCount.SliceCount = 0;
                FSuccessCount.SliceCount = 0;
                FErrorCount.SliceCount = 0;
            }
        }
    }
}
