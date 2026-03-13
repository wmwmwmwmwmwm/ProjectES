using DG.Tweening;
using Naninovel;
using Naninovel.Commands;
using Naninovel.Parsing;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StoryManager : Singleton<StoryManager>
{
	public Script _Script;

	protected override void Init()
	{
	}

	[Button("aa")]
	public void aa()
	{
		ScriptParser parser = new();
		//List<IScriptLine> lines = parser.ParseText(_Script.);
		print(_Script);
		var coms = _Script.ExtractCommands();
		foreach (var com in coms)
		{
			print($"{com.GetType()}  {com.PlaybackSpot}  {com.Wait}");
			if(com is PrintText printtext)
			{
				print($"{printtext.AuthorId}  {printtext.Text.Value}  {printtext.Text.Value.Parts}");
				foreach (var item in printtext.Text.Value.Parts)
				{
					print($"{_Script.TextMap.GetTextOrNull(item.Id)}");
				}
			}
		}
	}
}
