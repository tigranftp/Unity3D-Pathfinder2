using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//  Этот класс описывает поведение для "тела" игрока - капсулы (Capsule)
//    и предполагаем, что мы пишем код для этой капсулы - нам доступны её поля, дочерние объекты и прочее
//  Обращаться из этого скрипта к другим объектам сцены немного сложнее, но возможно
public class keyBoardControl : MonoBehaviour
{
    //  Поле _charController – ссылка на компонент CharacterController, который мы добавили к "телу" игрока
    private CharacterController _charController;

    //  Скорость перемещения - вещественное поле, причём оно public, поэтому его будет видно в редакторе Unity
    public float speed = 6.0f;
    
    // Функция Start запускается один раз – при первом запуске скрипта (похожа на конструктор)
    void Start()
    {
        //  При первом запуске находим компонент 
        _charController = GetComponent<CharacterController>();
    }

    // Функция Update вызывается каждый раз при рисовании кадра
    void Update()
    {
        //return;
        //  Получаем значения перемещения от клавиатуры в двух плоскостях, домножаем на скорость - это числа
        float deltaX = Input.GetAxis("Horizontal") * speed;
        float deltaZ = Input.GetAxis("Vertical") * speed;
        
        //  Создаём вектор - перемещать трёхмерные объекты надо с указанием трёх координат
        Vector3 movement = new Vector3(deltaX, 0, deltaZ);
        //  Ограничиваем величину перемезщения
        movement = Vector3.ClampMagnitude(movement, speed);
        //  Домножаем на прошедшее время - кадры могут рисоваться не с одинаковым интервалом времени
        movement *= Time.deltaTime;
        Debug.Log(movement.ToString());
        //  Преобразуем вектор направления из локальных координат в глобальные
        //GetComponent<Rigidbody>().AddForce(movement);
        //return;
        movement = transform.TransformDirection(movement);
        //  Выполняем перемещение игрок в соответствии с указанным вектором перемещения
        // GetComponent<Rigidbody>().MovePosition(movement);
        
        _charController.Move(movement);

    }
}
