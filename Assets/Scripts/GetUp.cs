using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetUp : MonoBehaviour
{
    [SerializeField] float force = 5.0f;
    [SerializeField] float max_angle = 20.0f;

    void Update()
    {
        //  Проверяем наклон бота, и при необходимости добавляем силу, тянущую вверх за "макушку" - чтобы не падал
        var angle = Vector3.Angle(Vector3.up, transform.up);
        if (angle > max_angle)
        {
            GetComponent<Rigidbody>().AddForceAtPosition(force*Vector3.up, transform.position + transform.up, ForceMode.Force);
        };
    }
}
