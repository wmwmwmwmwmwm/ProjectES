using DG.Tweening;
using Naninovel;
using Naninovel.Commands;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SingletonManager;

public class StoryManager : Singleton<StoryManager>
{
	public GameObject _Background;
	public GameObject _Character;
	public GameObject _TextBox;
	public TMP_Text _NameText;
	public TMP_Text _MainText;
	public Button _NextButton;

	public Script _Script;

	bool _Next;

	protected override void Init()
	{
		_NextButton.onClick.AddListener(NextButton);

		_Background.SetActive(false);
		_Character.SetActive(false);
		_TextBox.SetActive(false);

		StartCoroutine(ShowStory());
	}

	public IEnumerator ShowStory()
	{
		_Background.SetActive(true);
		_Character.SetActive(true);
		_TextBox.SetActive(true);
		List<Command> commands = _Script.ExtractCommands();
		foreach (Command com in commands)
		{
			Func<Command, IEnumerator> func = null;
			if (com is PrintText) func = ExecutePrintText;

			print(com.GetType());
			yield return StartCoroutine(func(com));
		}
		_Background.SetActive(false);
		_Character.SetActive(false);
		_TextBox.SetActive(false);
	}

	IEnumerator ExecutePrintText(Command command)
	{
		PrintText c = command as PrintText;
		_NameText.text = c.AuthorId;
		foreach (LocalizableTextPart part in c.Text.Value.Parts)
		{
			string text = _Script.TextMap.GetTextOrNull(part.Id);
			for (int i = 0; i < text.Length; i++)
			{
				_MainText.text = text[..i];
				yield return new WaitForSeconds(0.03f);
			}
			_Next = false;
			yield return new WaitUntil(() => _Next);
			_Next = false;
		}
	}

	void NextButton()
	{
		_Next = true;
	}

	[Button("aa")]
	public void aa()
	{
		//print(_Script);
		//var coms = _Script.ExtractCommands();
		//foreach (var com in coms)
		//{
		//	print($"{com.GetType()}  {com.PlaybackSpot}  {com.Wait}");
		//	if(com is PrintText printtext)
		//	{
		//		print($"{printtext.AuthorId}  {printtext.Text.Value}  {printtext.Text.Value.Parts}");
		//		foreach (var item in printtext.Text.Value.Parts)
		//		{
		//			print($"{_Script.TextMap.GetTextOrNull(item.Id)}");
		//		}
		//	}
		//}
	}
}
