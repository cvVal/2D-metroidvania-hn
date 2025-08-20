using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float m_followSpeed = 0.1f;
    [SerializeField] private Vector3 m_offset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3
            .Lerp(
                transform.position,
                PlayerController.Instance.transform.position + m_offset,
                m_followSpeed
            );
    }
}
