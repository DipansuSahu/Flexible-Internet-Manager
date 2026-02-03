using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InternetUserExample : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image statusIcon;

    private void Awake()
    {
        FlexibleInternetManager.Instance.OnInternetConnected += OnConnected;
        FlexibleInternetManager.Instance.OnInternetDisconnected += OnDisconnected;
    }

    private void OnConnected()
    {
        statusText.text = "Internet is ON";
        statusText.color = Color.green;
        statusIcon.color = Color.green;

        Debug.Log("Internet is ON");
    }

    private void OnDisconnected()
    {
        statusText.text = "Internet is OFF";
        statusText.color = Color.red;
        statusIcon.color = Color.red;

        Debug.Log("Internet is OFF");
    }

    private void OnDestroy()
    {
        if (FlexibleInternetManager.Instance != null)
        {
            FlexibleInternetManager.Instance.OnInternetConnected -= OnConnected;
            FlexibleInternetManager.Instance.OnInternetDisconnected -= OnDisconnected;
        }
    }
}