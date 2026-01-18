using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class VictoryScreen : MonoBehaviour
{
    [Header("FOUND ITENS")]
    [SerializeField] List<GameObject> foundItensPrefabs = new List<GameObject>();
    [SerializeField] Transform foundItensContainer;
    [Header("XP EARNED")]
    [SerializeField] GameObject[] playerXPItensPrefabs;
    [SerializeField] Transform playerXPItensContainer;

    [SerializeField] GameObject screen;

    [Header("BUTTONS")]
    [SerializeField] GameObject restartButton;
    [SerializeField] GameObject quitButton;
    [SerializeField] GameObject continueButton; // New button for RPG flow

    CanvasGroup restartButtonCanvasGroup;
    CanvasGroup quittButtonCanvasGroup;
    CanvasGroup continueButtonCanvasGroup;

    private void OnEnable()
    {
        GameManager.OnGameWon += ShowScreen;
    }

    private void OnDisable()
    {
        GameManager.OnGameWon -= ShowScreen;

    }

    void Start()
    {
        if (restartButton) restartButton.SetActive(false);
        if (quitButton) quitButton.SetActive(false);
        if (continueButton) continueButton.SetActive(false);

        if (restartButton) restartButton.GetComponent<Button>().onClick.AddListener(GameManager.instance.RestartCurrentScene);
        if (quitButton) quitButton.GetComponent<Button>().onClick.AddListener(GameManager.instance.QuitGame);

        if (continueButton)
        {
            continueButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (EncounterManager.instance != null)
                    EncounterManager.instance.EndEncounter(true);
                else
                    GameManager.instance.LoadMenuScene(); // Fallback
            });
            continueButtonCanvasGroup = continueButton.GetComponent<CanvasGroup>();
        }

        if (restartButton) restartButtonCanvasGroup = restartButton.GetComponent<CanvasGroup>();
        if (quitButton) quittButtonCanvasGroup = quitButton.GetComponent<CanvasGroup>();

    }


    public void ShowScreen()
    {
        StartCoroutine(ShowScreenCoroutine());
        ShowXPEarned();
    }

    IEnumerator ShowScreenCoroutine()
    {
        if (EncounterManager.instance != null)
            EncounterManager.instance.PreloadWorldScene();

        yield return new WaitForSeconds(2.75f);
        screen.SetActive(true);


        int _createdItensAmount = 3;

        for (int i = 0; i < _createdItensAmount; i++)
        {
            int _randomIndex = Random.Range(0, foundItensPrefabs.Count);
            GameObject _randomLootItem = Instantiate(foundItensPrefabs[_randomIndex], foundItensContainer);
            foundItensPrefabs.RemoveAt(_randomIndex);

            CanvasGroup _itemCanvas = _randomLootItem.GetComponent<CanvasGroup>();

            _itemCanvas.alpha = 0;

            while (_itemCanvas.alpha < 1)
            {
                _itemCanvas.alpha += .1f;
                yield return null;
            }

            _randomLootItem.transform.DOScale(new Vector3(1.1f, 1.1f, 1), .2f).SetEase(Ease.OutBack);

            yield return new WaitForSeconds(.25f);
        }

        yield return new WaitForSeconds(0.5f);

        if (GameManager.IsOnline() && !NetworkManager.Singleton.IsServer)
            yield break;

        // RPG Flow: If EncounterManager exists, show Continue.
        if (EncounterManager.instance != null && continueButton != null)
        {
            continueButtonCanvasGroup.alpha = 0;
            continueButton.SetActive(true);
            continueButtonCanvasGroup.DOFade(1, .25f);
            EventSystem.current.SetSelectedGameObject(continueButton);
        }
        else
        {
            // Default Flow (Arcade/Roguelite)
            if (restartButton)
            {
                restartButtonCanvasGroup.alpha = 0;
                restartButton.SetActive(true);
                restartButtonCanvasGroup.DOFade(1, .25f);
                EventSystem.current.SetSelectedGameObject(restartButton);
            }
            if (quitButton)
            {
                quittButtonCanvasGroup.alpha = 0;
                quitButton.SetActive(true);
                quittButtonCanvasGroup.DOFade(1, .25f);
            }
        }
    }

    void ShowXPEarned()
    {
        int _xpItensCreated = CombatManager.instance.PlayersOnField.Count;

        for (int i = 0; i < _xpItensCreated; i++)
        {
            GameObject g = Instantiate(playerXPItensPrefabs[i], playerXPItensContainer);
        }
    }
}
