using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Poker.Client.Infrastructure.UI.Views;

namespace Poker.Client.Infrastructure.Diagnostics.DevPanel
{
	public sealed class DevPanelView : MonoBehaviour, IDevPanelView
	{
		private Button _toggle;
		private Button _bugReport;
		private Button _shortDrop;
		private Button _longDrop;
		private GameObject _panel;
		private TMP_Text _info;
		private RectTransform _actions;

		public event Action ToggleClicked;
		public event Action BugReportClicked;
		public event Action ShortDropClicked;
		public event Action LongDropClicked;

		public bool IsPanelVisible => _panel.activeSelf;

		public void Bind(Button toggle, GameObject panel, TMP_Text info, Button bugReport, Button shortDrop, Button longDrop, RectTransform actions)
		{
			_actions = actions;
			_toggle = toggle;
			_panel = panel;
			_info = info;
			_bugReport = bugReport;
			_shortDrop = shortDrop;
			_longDrop = longDrop;
			_toggle.onClick.AddListener(OnToggleClicked);
			_bugReport.onClick.AddListener(OnBugReportClicked);
			_shortDrop.onClick.AddListener(OnShortDropClicked);
			_longDrop.onClick.AddListener(OnLongDropClicked);
		}

		public void SetPanelVisible(bool isVisible) => _panel.SetActive(isVisible);

		public void SetInfo(string info) => _info.text = info;

		public void AddAction(DevPanelAction action)
		{
			Button button = UiBuilder.CreateButton(_actions, action.Label, action.Label);
			button.onClick.AddListener(action.Run.Invoke);
		}

		private void OnToggleClicked() => ToggleClicked?.Invoke();

		private void OnBugReportClicked() => BugReportClicked?.Invoke();

		private void OnShortDropClicked() => ShortDropClicked?.Invoke();

		private void OnLongDropClicked() => LongDropClicked?.Invoke();

		private void OnDestroy()
		{
			if (_toggle == null) return;

			_toggle.onClick.RemoveListener(OnToggleClicked);
			_bugReport.onClick.RemoveListener(OnBugReportClicked);
			_shortDrop.onClick.RemoveListener(OnShortDropClicked);
			_longDrop.onClick.RemoveListener(OnLongDropClicked);
		}
	}
}