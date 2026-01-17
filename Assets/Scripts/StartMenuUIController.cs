using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.InputSystem;
using System.Linq;

public class StartMenuUIController : MonoBehaviour
{
    [Serializable]
    public class Content
    {
        public Transform contentContainer;
        public Button[] contentButtons;
        public TMP_InputField contentInputField;
    }


    [SerializeField] Content[] contents;
    [Space(10)]
    [SerializeField] TextMeshProUGUI[] objsToFadeOut;
    [Space(10)]
    [Header("Texts")]
    [SerializeField] TextMeshProUGUI errorMessage;
    [SerializeField] TextMeshProUGUI connectingSign;
    [SerializeField] GameObject pressToStart;
    [SerializeField] CanvasGroup createJoinLobbyContent;
    [SerializeField] DustStormTransition dustStorm;
    [SerializeField] Image fader;

    public static Action OnPlayerChoseCoop;

    Content currentContent;

    EventSystem eventSystem;

    private void Awake()
    {
        FadeFromBlack();

        connectingSign.DOFade(0, 0);
        connectingSign.gameObject.SetActive(false);
        pressToStart.SetActive(true);
        createJoinLobbyContent.DOFade(0, 0);
        createJoinLobbyContent.gameObject.SetActive(false);

        eventSystem = EventSystem.current;
    }

    void Start()
    {
        AssignButtons();
        HideContents();
        HideErrorMessage();
    }

    public void OnMenus_Back(InputValue value)
    {
        if (currentContent != contents[0] && currentContent != contents[4])
        {
            HideContents();
            HideErrorMessage();

            Sequence _sequence = DOTween.Sequence();
            _sequence.AppendInterval(.2f);
            _sequence.OnComplete(() => pressToStart.SetActive(true));
        }
    }
    
    public void OnQuitToDesktop(InputValue value)
    {
        Application.Quit();
    }

    public void OnAnyPositiveKey(InputValue value)
    {
        if (pressToStart.activeSelf)
        {
            pressToStart.SetActive(false);
            ShowContent(0);
        }
    }

    void FadeFromBlack()
    {
        fader.gameObject.SetActive(true);
        Sequence _fadeFromBlack = DOTween.Sequence();
        _fadeFromBlack.Append(fader.DOFade(1f, 0f));
        _fadeFromBlack.AppendInterval(.25f);
        _fadeFromBlack.Append(fader.DOFade(0f, 2f)).OnComplete(() => fader.gameObject.SetActive(false));
    }

    void HideContents(bool hideGameTitle = false)
    {
        foreach (var c in contents)
            c.contentContainer.gameObject.SetActive(false);
    }

    void ShowContent(int index)
    {
        StartCoroutine(ShowContent_Coroutine(index));
    }

    IEnumerator ShowContent_Coroutine(int index)
    {
        float _delay = 0;

        if (index == 1)
        {
            connectingSign.DOFade(0, .2f).OnComplete(() => connectingSign.gameObject.SetActive(false));
            _delay = .2f;
        }

        yield return new WaitForSeconds(_delay);
        DisableContentButtons(index);
        HideButtonArrowsAndDeselectHighlightedButtons(index);
        yield return new WaitForSeconds(.05f);
        ActivateContentContainer(index);
        SelectFirstButton(index);
        SetCurrentContent(index);
        StartCoroutine(EnableButtonsDelayed(index));
    }

    void DisableContentButtons(int index)
    {
        if (contents[index].contentButtons.Length == 0)
            return;

        foreach (Button button in contents[index].contentButtons)
        {
            button.interactable = false;
        }
    }

    void ActivateContentContainer(int index)
    {
        foreach (Content c in contents)
        {
            c.contentContainer.gameObject.SetActive(c == contents[index]);
        }

        contents[index].contentContainer.GetComponent<CanvasGroup>().alpha = 1;
    }

    void SetCurrentContent(int index) => currentContent = contents[index];
    void SelectFirstButton(int index) 
    {
        if (contents[index].contentButtons.Length == 0) 
            return;

        eventSystem.SetSelectedGameObject(contents[index].contentButtons[0].gameObject);
    } 

    void HideButtonArrowsAndDeselectHighlightedButtons(int index)
    {
        foreach (Button button in contents[index].contentButtons.Skip(1))
        {
            GameObject _buttonArrow = button.transform.GetChild(1).gameObject;
            _buttonArrow.SetActive(false);

            HighlightButton _highlightButton = button.GetComponent<HighlightButton>();
            if (_highlightButton != null)
                _highlightButton.Deselected();
            
        }
    }

    IEnumerator EnableButtonsDelayed(int index)
    {
        yield return new WaitForSeconds(.1f);
        foreach (Button b in contents[index].contentButtons)
        {
            b.interactable = true;
        }
    }

    void AssignButtons()
    {
        contents[0].contentButtons[0].onClick.AddListener(delegate { FadeToDustAndLoadScene("World"); });
        contents[0].contentButtons[1].onClick.AddListener(delegate { FadeToDustAndLoadScene(2); });
    }

    public void FadeToDustAndLoadScene(int sceneIndex)
    {
        foreach (TextMeshProUGUI item in objsToFadeOut)
        {
            var _canvasGrp = item.GetComponent<CanvasGroup>();

            if (_canvasGrp)
                _canvasGrp.DOFade(0, 0.5f);
            else item.DOFade(0, 0.5f);
        }

        Sequence fadeAndLoadSequence = DOTween.Sequence();
        fadeAndLoadSequence.Append(currentContent.contentContainer.GetComponent<CanvasGroup>().DOFade(0, .5f));
        dustStorm.PlayParticles();
        fadeAndLoadSequence.AppendInterval(2.25f);
        fadeAndLoadSequence.OnComplete(() => SceneManager.LoadScene(sceneIndex));
    }

    public void FadeToDustAndLoadScene(string sceneName)
    {
        foreach (TextMeshProUGUI item in objsToFadeOut)
        {
            var _canvasGrp = item.GetComponent<CanvasGroup>();

            if (_canvasGrp)
                _canvasGrp.DOFade(0, 0.5f);
            else item.DOFade(0, 0.5f);
        }

        Sequence fadeAndLoadSequence = DOTween.Sequence();
        fadeAndLoadSequence.Append(currentContent.contentContainer.GetComponent<CanvasGroup>().DOFade(0, .5f));
        dustStorm.PlayParticles();
        fadeAndLoadSequence.AppendInterval(2.25f);
        fadeAndLoadSequence.OnComplete(() => SceneManager.LoadScene(sceneName));
    }

    public void ShowErrorMessage(string text)
    {
        errorMessage.gameObject.SetActive(true);
        errorMessage.text = text;
    }

    public void HideErrorMessage() => errorMessage.gameObject.SetActive(false);

}
