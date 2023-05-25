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
        public float maxSpeed = 5;
        /// <summary>
        /// Максимальный угол поворота агента
        /// </summary>
        public float rotationAngle;
        /// <summary>
        /// Количество дискретных углов поворота
        /// </summary>
        public int angleSteps;
        /// <summary>
        /// Сила прыжка
        /// </summary>
        public float jumpForce;
        /// <summary>
        /// Длина прыжка - надо подобрать эмпирически, используется для построения пути
        /// </summary>
        public float jumpLength;
        /// <summary>
        /// эпсилон-окрестность точки, в пределах которой точка считается достигнутой
        /// </summary>
        public float epsilon = 0.1f;
        /// <summary>
        /// Дельта времени (шаг по времени), с которой строится маршрут
        /// </summary>
        public float deltaTime = 0.5f;
        /// <summary>
        /// Дельта пространства (сетки), с которой строится маршрут
        /// </summary>
        public float deltaDist = 0.5f;
    }
}
