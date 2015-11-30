#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	public class DerivedObject
	{
		public override string ToString()
		{
			return "DerivedObject created by Template (Node Source)";
		}
	}
	[PluginInfo(Name = "NoSpreadOutput", Category = "Boohoo", Help = "Basic template with one value in/out", Tags = "")]
	public class BoohooNoSpreadOutputNode : IPluginEvaluate
	{

		[Output("Output")]
		public ISpread<ISpread<DerivedObject>> FOutput;

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FOutput[0] = new Spread<DerivedObject>();
		}
	}
}
