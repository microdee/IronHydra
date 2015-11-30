using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using Microsoft.Scripting;

namespace VVVV.IronHydra.Nodes
{
    [PluginInfo(
        Name = "Compile",
        Category = "Python",
        Author = "microdee"
    )]
    public class PythonCompileNode : IPluginEvaluate//, IPartImportsSatisfiedNotification
    {
        [Input("Expression")]
        public ISpread<string> FExp;
        [Input("Code Type")]
        public ISpread<SourceCodeKind> FType;
        [Input("Compile", IsBang = true)]
        public ISpread<bool> FCompile;

        [Output("Output")]
        public ISpread<CompiledPythonResourceWithCode> FOutput;
        [Output("Working")]
        public ISpread<bool> FWorking;
        [Output("Success")]
        public ISpread<bool> FSuccess;
        [Output("Error")]
        public ISpread<bool> FError;
        [Output("Error Message")]
        public ISpread<string> FErrorMessage;

        private Spread<CompilationEvents> Events = new Spread<CompilationEvents>();

        public void Evaluate(int SpreadMax)
        {
            if(FCompile[0])
            {
                FOutput.SliceCount = SpreadMax;
                FWorking.SliceCount = SpreadMax;
                FSuccess.SliceCount = SpreadMax;
                FError.SliceCount = SpreadMax;
                FErrorMessage.SliceCount = SpreadMax;
                Events.SliceCount = SpreadMax;

                for(int i=0; i<FExp.SliceCount; i++)
                {
                    if(FCompile[i])
                    {
                        FWorking[i] = true;
                        FSuccess[i] = false;
                        FError[i] = false;
                        FErrorMessage[i] = "";

                        Events[i] = new CompilationEvents();
                        Events[i].ID = i;
                        Events[i].StatusBools.Add("Working", FWorking);
                        Events[i].StatusBools.Add("Success", FSuccess);
                        Events[i].StatusBools.Add("Error", FError);
                        Events[i].ErrorMessage = FErrorMessage;

                        FOutput[i] = new CompiledPythonResourceWithCode(FExp[i], FType[i]);
                        FOutput[i].OnCompiled += Events[i].OnCompiled;
                        FOutput[i].OnCompilationError += Events[i].OnError;
                        FOutput[i].CompilationTask.Start();
                    }
                }
            }
        }
    }
}
