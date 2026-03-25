using Animancer;
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
		_CharCamera.gameObject.SetActive(false);
		_NextButton.onClick.AddListener(NextButton);
		SetActivePanels(false);

		//CharactorCapture();
		//StartCoroutine(ShowStory());
	}

	public IEnumerator ShowStory()
	{
		SetActivePanels(true);
		List<Command> commands = _Script.ExtractCommands();
		foreach (Command com in commands)
		{
			Func<Command, IEnumerator> func = null;
			if (com is PrintText) func = ExecutePrintText;

			yield return StartCoroutine(func(com));
		}
		SetActivePanels(false);
	}

	IEnumerator ExecutePrintText(Command command)
	{
		PrintText c = command as PrintText;
		_NameText.text = c.AuthorId;
		foreach (LocalizableTextPart part in c.Text.Value.Parts)
		{
			string text = _Script.TextMap.GetTextOrNull(part.Id);
			for (int i = 1; i <= text.Length; i++)
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

	void SetActivePanels(bool on)
	{
		_Background.SetActive(on);
		_Character.SetActive(on);
		_TextBox.SetActive(on);
		_NextButton.gameObject.SetActive(on);
	}

	[Button("aa")]
	public void aa()
	{
		print("----");
		foreach (var line in _Script.Lines)
		{
			print($"{line.LineIndex}  {_Script.GetLabelForLine(line.LineIndex)}");
		}
		print("----");
		List<Command> coms = _Script.ExtractCommands();
		foreach (var com in coms)
		{
			print($"{_Script.GetLabelForLine(com.PlaybackSpot.LineIndex)} {com.GetType()}  {com.PlaybackSpot}  {com.Wait}");
			if (com is PrintText printtext)
			{
				print($"{printtext.AuthorId}  {printtext.Text.Value}  {printtext.Text.Value.Parts}");
				foreach (var item in printtext.Text.Value.Parts)
				{
					print($"{_Script.TextMap.GetTextOrNull(item.Id)}");
				}
			}
		}
	}

	public Camera _CharCamera;
	public GameObject _Astar, _Inasi;
	public Transform _CharParent;
	public AnimationClip _Clip;
	void CharactorCapture()
	{
		float a_xPos = 0.25f / 0.888f;
		float b_xPos = -0.25f / 0.888f;
		GameObject astar = Instantiate(_Astar);
		astar.transform.SetParent(_CharParent);
		astar.transform.SetLocalPositionAndRotation(new Vector3(a_xPos, 0f, 2.1f), Quaternion.Euler(0f, 180f, 0f));
		SetChar(astar);
		GameObject inasi = Instantiate(_Inasi, new Vector3(b_xPos, 0f, 1f), Quaternion.identity);
		inasi.transform.SetParent(_CharParent);
		inasi.transform.SetLocalPositionAndRotation(new Vector3(b_xPos, 0f, 2.1f), Quaternion.Euler(0f, 180f, 0f));
		SetChar(inasi);
		_CharCamera.Render();

		void SetChar(GameObject obj)
		{
			SoloAnimation anim = obj.TryAddComponent<SoloAnimation>();
			anim.Animator = obj.GetComponent<Animator>();
			anim.Clip = _Clip;
			anim.Play();
			anim.Evaluate();
		}
	}
}
