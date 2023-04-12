using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Script.Comm;
using UnityEngine.SceneManagement;
using Assets.Script.Player;

public class Connector : MonoBehaviour
{

    [Header("Components")]
    [SerializeField] private RectTransform backGround;
    [SerializeField] private Button startConn;

    [Space(5)]

    [Header("Local")]
    [SerializeField] private Toggle exchangeLocalIP;
    [SerializeField] private TMP_InputField localIPField;

    [Space(5)]
    
    [Header("External")]
    [SerializeField] private List<TMP_InputField> connectIPFieldList;
    [SerializeField] private Button minusConnectInput;
    [SerializeField] private Button plusConnectInput;
    
    [Space(5)]

    [Header("Parameters")]
    [SerializeField] private int maxConnectionNumber;

    bool changeLocal;
    private string[] externalIPs;
    private string localIP;

    void Start()
    {
        minusConnectInput.onClick.AddListener(() =>
        {
            if (connectIPFieldList.Count <= 1) return;

            TMP_InputField temp = connectIPFieldList[^1];

            connectIPFieldList.RemoveAt(connectIPFieldList.Count - 1);

            Destroy(temp.gameObject);

            float sizeY = connectIPFieldList[0].GetComponent<RectTransform>().rect.height * 1.5f;

            backGround.sizeDelta -= new Vector2(0, sizeY);
            backGround.anchoredPosition += new Vector2(0, sizeY * 0.5f );
        });

        plusConnectInput.onClick.AddListener(() =>
        {
            if (connectIPFieldList.Count >= maxConnectionNumber) return;

            TMP_InputField temp = Instantiate(connectIPFieldList[0], connectIPFieldList[0].transform.parent);
            temp.text = "";
            RectTransform tempRectTransform = temp.GetComponent<RectTransform>();
            tempRectTransform.anchoredPosition += new Vector2(0, tempRectTransform.anchoredPosition.y * 1.5f * connectIPFieldList.Count);

            connectIPFieldList.Add(temp);

            float sizeY = connectIPFieldList[0].GetComponent<RectTransform>().rect.height * 1.5f;

            backGround.sizeDelta += new Vector2(0, sizeY);
            backGround.anchoredPosition -= new Vector2(0, sizeY * 0.5f);
        });

        exchangeLocalIP.onValueChanged.AddListener(state => 
        {
            changeLocal = state;

            localIPField.gameObject.SetActive(state);

        });

        startConn.onClick.AddListener(StartConnection);

    }

    void StartConnection()
    {
        localIP = changeLocal && localIPField.text.Length > 0 ? localIPField.text : "127.0.0.1:50345";

        List<string> ipList = new();
        foreach (var iter in connectIPFieldList)
        {
            if (iter.text.Length > 0) { ipList.Add(iter.text); }
        }

        externalIPs = ipList.ToArray();

        SceneManager.LoadScene("Main_game", LoadSceneMode.Additive);
        StartCoroutine(nameof(OnSceneLoad));
    }

    IEnumerator OnSceneLoad()
    {

        while (true)
        {

            try
            {
                SceneManager.SetActiveScene(SceneManager.GetSceneByName("Main_game"));
                RaftInitializer.Instance.InitiazeRaftServer(localIP, externalIPs);
                Interpreter.Instance.Register(localIP, "");
                SceneManager.UnloadSceneAsync("Main_menu");
                break;
            }
            catch (System.ArgumentException)
            {
                ;
            }    

            yield return null;
        }
    }

}
