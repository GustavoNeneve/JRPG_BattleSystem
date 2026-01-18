using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] GameObject screen;
    [SerializeField] Transform gameOverPanel;
    [SerializeField] GameObject firstSelected;

    private CanvasGroup screenCanvasGroup;
    private RectTransform gameOverPanelRectTransform;

    void Start()
    {
        screen.SetActive(false);

        screenCanvasGroup = screen.GetComponent<CanvasGroup>();
        gameOverPanelRectTransform = gameOverPanel.GetComponent<RectTransform>();
    }

    public void ShowGameOverScreen()
    {
        screenCanvasGroup.alpha = 0;
        screen.SetActive(true);
        StartCoroutine(ShowScreenCoroutine());
        gameOverPanelRectTransform.DOAnchorPosY(0, .2f);
    }

    IEnumerator ShowScreenCoroutine()
    {
        EventSystem _eventSystem = EventSystem.current;

        _eventSystem.SetSelectedGameObject(firstSelected);

        while (screenCanvasGroup.alpha < 1)
        {
            screenCanvasGroup.alpha += Time.deltaTime * 5f;
            yield return null;
        }
    }

    public void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void ReturnToHospital()
    {
        if (EncounterManager.instance != null)
        {
            EncounterManager.instance.EndEncounter(false);
        }
        else
        {
            SceneManager.LoadScene(0); // Main Menu or similar fallback
        }
    }
}
