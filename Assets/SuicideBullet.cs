using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuicideBullet : MonoBehaviour
{
    // Это скрипт самоуничтожения снаряда (ни или что-то около того)
    private float startTime;
    private Vector3 startPosition;
    public float LifeTime;

    void Start()
    {
        //  Время с начала игры
        startTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        // Если время, прошедшее с начала игры, превышает некоторый интервал - убиваемся
        if (Time.time - startTime > LifeTime) Destroy(gameObject);
    }

    public void SetStartTankPosition(Vector3 StartPosition)
    {
        startPosition = StartPosition;
    }
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Bullet landed : " + (Time.time - startTime) + " sec; Distance : " + Vector3.Distance(transform.position, startPosition));
    }

}
