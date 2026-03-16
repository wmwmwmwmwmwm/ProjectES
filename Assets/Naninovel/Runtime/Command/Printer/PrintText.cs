using System.Collections;
using UnityEngine;

namespace Naninovel.Commands
{
	[CommandAlias("print")]
	public class PrintText : Command
	{
		[ParameterAlias(NamelessParameterAlias), RequiredParameter]
		public LocalizableTextParameter Text;
		[ParameterAlias("author")]
		public StringParameter AuthorId;
		[ParameterAlias("waitInput")]
		public BooleanParameter WaitForInput;

		public override UniTask ExecuteAsync(AsyncToken asyncToken = default)
		{
			throw new System.NotImplementedException();
		}
	}
}
