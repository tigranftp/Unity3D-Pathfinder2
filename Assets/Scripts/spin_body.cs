using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//  Скрипт для вращения тела игрока в горизонтальной плоскости (вокруг оси Y)
public class spin_body : MonoBehaviour
{
    //  Скорость вращения (аналог чувствительности мыши)
    public float sensitivityHoriz = 9.0f;
    
    //  Текущий угол поворота - он запоминается, хотя можно обойтись и без него
    private float _rotationY = 0;

    // Начальный метод пустой - инициализировать нечего
    // void Start()  { }

    // Каждый раз при обновлении считываем позицию курсора мыши, и соответственно выполняем вращение
    void Update()
    {
        _rotationY += Input.GetAxis("Mouse X") * sensitivityHoriz;

        //GetComponent<Rigidbody>().isKinematic = false;
        //transform.localEulerAngles = new Vector3(0, _rotationY, 0);
        //GetComponent<Rigidbody>().isKinematic = true;

        Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, _rotationY, 0) * Time.deltaTime);
        var rb = GetComponent<Rigidbody>();
        //rb.rotation = rb.rotation * deltaRotation;
        rb.rotation = deltaRotation;
    }
}
