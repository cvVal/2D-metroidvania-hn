using UnityEngine;
using UnityEngine.UI;

public class HeartController : MonoBehaviour
{
    private PlayerController m_player;

    private GameObject[] m_heartContainers;
    private Image[] m_heartFills;
    public Transform HeartsParent;
    public GameObject HeartContainerPrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_player = PlayerController.Instance;
        m_heartContainers = new GameObject[PlayerController.Instance.MaxHealth];
        m_heartFills = new Image[PlayerController.Instance.MaxHealth];

        PlayerController.Instance.OnHealthChangedCallback += UpdateHeartsHUD;
        InstantiateHeartContainers();
        UpdateHeartsHUD();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void SetHeartContainers()
    {
        for (int i = 0; i < m_heartContainers.Length; i++)
        {
            if (i < PlayerController.Instance.MaxHealth)
            {
                m_heartContainers[i].SetActive(true);
            }
            else
            {
                m_heartContainers[i].SetActive(false);
            }
        }
    }

    void SetFilledHearts()
    {
        for (int i = 0; i < m_heartFills.Length; i++)
        {
            if (i < PlayerController.Instance.Health)
            {
                m_heartFills[i].fillAmount = 1;
            }
            else
            {
                m_heartFills[i].fillAmount = 0;
            }
        }
    }

    void InstantiateHeartContainers()
    {
        for (int i = 0; i < PlayerController.Instance.MaxHealth; i++)
        {
            GameObject temp = Instantiate(HeartContainerPrefab);
            temp.transform.SetParent(HeartsParent, false);
            m_heartContainers[i] = temp;
            m_heartFills[i] = temp.transform.Find("HeartFill").GetComponent<Image>();
        }
    }

    void UpdateHeartsHUD()
    {
        SetHeartContainers();
        SetFilledHearts();
    }
}
