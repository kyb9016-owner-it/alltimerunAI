using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class AiPetGamePrototype : MonoBehaviour
{
    private enum ViewState
    {
        Home,
        Run,
        Result
    }

    private Canvas _canvas;
    private GameObject _homePanel;
    private GameObject _runPanel;
    private GameObject _resultPanel;
    private Text _scoreText;
    private Text _resultText;
    private int _score;

    private void Start()
    {
        EnsureEventSystem();
        BuildUI();
        Show(ViewState.Home);
    }

    private void EnsureEventSystem()
    {
        var es = FindObjectOfType<EventSystem>();
        if (es == null)
        {
            var esObject = new GameObject("EventSystem");
            es = esObject.AddComponent<EventSystem>();
        }

        // Support projects using the new Input System package.
        var inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
        {
            if (es.GetComponent(inputSystemModuleType) == null)
            {
                es.gameObject.AddComponent(inputSystemModuleType);
            }
            var legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                Destroy(legacy);
            }
            return;
        }

        if (es.GetComponent<BaseInputModule>() == null)
        {
            es.gameObject.AddComponent<StandaloneInputModule>();
        }
    }

    private void BuildUI()
    {
        _canvas = FindObjectOfType<Canvas>();
        if (_canvas == null)
        {
            var canvasObject = new GameObject("Canvas");
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        var scaler = _canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = _canvas.gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        if (_canvas.GetComponent<GraphicRaycaster>() == null)
        {
            _canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        _homePanel = CreatePanel("HomePanel", new Color(0.95f, 0.90f, 0.95f));
        _runPanel = CreatePanel("RunPanel", new Color(0.88f, 0.95f, 0.90f));
        _resultPanel = CreatePanel("ResultPanel", new Color(0.90f, 0.92f, 0.98f));

        BuildHomeUI();
        BuildRunUI();
        BuildResultUI();
    }

    private GameObject CreatePanel(string name, Color color)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(_canvas.transform, false);
        var image = panel.AddComponent<Image>();
        image.color = color;

        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }

    private void BuildHomeUI()
    {
        CreateText(_homePanel, "Title", "AI 육성 러너", 64, new Vector2(0, 240));
        CreateText(_homePanel, "Subtitle", "귀여운 캐주얼 프로토타입", 32, new Vector2(0, 170));
        CreateButton(_homePanel, "StartButton", "Start Run", new Vector2(0, -20), OnStartRun);
    }

    private void BuildRunUI()
    {
        CreateText(_runPanel, "RunTitle", "In-Run", 56, new Vector2(0, 260));
        _scoreText = CreateText(_runPanel, "ScoreText", "Score: 0", 44, new Vector2(0, 160));
        CreateButton(_runPanel, "GainButton", "+10 Score", new Vector2(0, -20), OnGainScore);
        CreateButton(_runPanel, "FailButton", "Fail", new Vector2(0, -110), OnFailRun);
    }

    private void BuildResultUI()
    {
        CreateText(_resultPanel, "ResultTitle", "Result", 56, new Vector2(0, 260));
        _resultText = CreateText(_resultPanel, "ResultText", "Final Score: 0", 44, new Vector2(0, 150));
        CreateButton(_resultPanel, "RetryButton", "Retry", new Vector2(0, -20), OnStartRun);
        CreateButton(_resultPanel, "HomeButton", "Home", new Vector2(0, -110), () => Show(ViewState.Home));
    }

    private Text CreateText(GameObject parent, string name, string value, int fontSize, Vector2 pos)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent.transform, false);
        var text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.15f, 0.15f, 0.15f);
        text.raycastTarget = false;

        var rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900, 120);
        rect.anchoredPosition = pos;
        return text;
    }

    private void CreateButton(GameObject parent, string name, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent.transform, false);
        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.23f, 0.49f, 0.94f);
        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320, 90);
        rect.anchoredPosition = pos;

        var labelText = CreateText(buttonObject, "Label", label, 32, Vector2.zero);
        labelText.color = Color.white;
    }

    private void OnStartRun()
    {
        _score = 0;
        _scoreText.text = "Score: 0";
        Show(ViewState.Run);
    }

    private void OnGainScore()
    {
        _score += 10;
        _scoreText.text = "Score: " + _score;
    }

    private void OnFailRun()
    {
        _resultText.text = "Final Score: " + _score;
        Show(ViewState.Result);
    }

    private void Show(ViewState state)
    {
        _homePanel.SetActive(state == ViewState.Home);
        _runPanel.SetActive(state == ViewState.Run);
        _resultPanel.SetActive(state == ViewState.Result);
    }
}
