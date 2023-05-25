using BaseAI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform1Movement : MonoBehaviour
{
    [SerializeField] private bool moving;
    public Vector3 rotationCenter;
    public float rotationSpeed = 1.0f;
    public float rotationRadius = 10f;


    void Start()
    {
        rotationCenter = transform.position + rotationRadius * Vector3.left;
    }

    void Update()
    {
        if (!moving) return;

        transform.RotateAround(rotationCenter, Vector3.up, Time.deltaTime*rotationSpeed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
            other.transform.SetParent(transform);
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            other.transform.SetParent(null);
        }
    }
}
