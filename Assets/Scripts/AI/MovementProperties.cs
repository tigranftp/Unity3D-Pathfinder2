using System;
using UnityEngine;

namespace BaseAI
{
    /// <summary>
    /// Параметры движения агента - как может поворачивать, какие шаги делать
    /// </summary>
    [Serializable]
    public class MovementProperties
    {
        /// <summary>
        /// Максимальная скорость движения агента
        /// </summary>
        public float maxSpeed;
        /// <summary>
        /// Шаг поворота агента в градусах
        /// </summary>
        public float rotationAngle;
        /// <summary>
        /// Количество дискретных углов поворота в одну сторону. 0 - только движение вперёд, 1 - влево/прямо/вправо, и т.д.
        /// </summary>
        public int angleSteps;
        /// <summary>
        /// Длина прыжка (фиксированная)
        /// </summary>
        public float jumpLength;
        /// <summary>
        /// Время прыжка - предварительно рассчитать!
        /// </summary>
        public float jumpTime;
        /// <summary>
        /// Сила прыжка
        /// </summary>
        public float jumpForce;
        /// <summary>
        /// эпсилон-окрестность точки, в пределах которой точка считается достигнутой
        /// </summary>
        public float epsilon = 0.1f;
        /// <summary>
        /// Дельта времени (шаг по времени), с которой строится маршрут
        /// </summary>
        public float deltaTime = 1f;
        /// <summary>
        /// Шаг по пространству, с которым происходит дискретизация области (для отсечения посещённых точек)
        /// </summary>
        public float deltaDist = 1f;
    }
}
