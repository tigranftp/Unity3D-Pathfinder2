using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BaseAI
{
    /// <summary>
    /// Точка пути - изменяем по сравенению с предыдущим проектом
    /// </summary>
    public class PathNode
    {
        /// <summary>
        /// Позиция в глобальных координатах
        /// </summary>
        public Vector3 Position { get; set; }
        /// <summary>
        /// Направление
        /// </summary>
        public Vector3 Direction { get; set; }
        /// <summary>
        /// Момент времени
        /// </summary>
        public float TimeMoment { get; set; }
        /// <summary>
        /// Явно заданный регион
        /// </summary>
        public int? RegionIndex { get; set; }
        /// <summary>
        /// Длина части маршртута, зашедшего в другой регион
        /// </summary>
        public byte OtherRegionLength { get; set; } = 0;

        /// <summary>
        /// Родительская вершина - предшествующая текущей в пути от начальной к целевой
        /// </summary>
        public PathNode Parent { get; set; } = null;

        public float F { get; set; }  //  Пройденный путь от цели

        /// <summary>
        /// Конструирование вершины на основе родительской (если она указана)
        /// </summary>
        /// <param name="ParentNode">Если существует родительская вершина, то её указываем</param>
        public PathNode(PathNode ParentNode = null)
        {
            Parent = ParentNode;
        }

        /// <summary>
        /// Конструирование вершины на основе родительской (если она указана)
        /// </summary>
        /// <param name="ParentNode">Если существует родительская вершина, то её указываем</param>
        public PathNode(Vector3 currentPosition, Vector3 currentDirection)
        {
            Position = currentPosition;      //  Позицию задаём
            Direction = currentDirection;    //  Направление отсутствует
            TimeMoment = Time.fixedTime;     //  Время текущее
            Parent = null;                   //  Родителя нет
            F = 0;
        }

        /// <summary>
        /// Расстояние между точками без учёта времени. Со временем - отдельная история
        /// Если мы рассматриваем расстояние до целевой вершины, то непонятно как учитывать время
        /// </summary>
        /// <param name="other">Точка, до которой высчитываем расстояние</param>
        /// <returns></returns>
        public float Distance(PathNode other)
        {
            return Vector3.Distance(Position, other.Position);
        }

        /// <summary>
        /// Расстояние между точками без учёта времени. Со временем - отдельная история
        /// Если мы рассматриваем расстояние до целевой вершины, то непонятно как учитывать время
        /// </summary>
        /// <param name="other">Точка, до которой высчитываем расстояние</param>
        /// <returns></returns>
        public float Distance(Vector3 other)
        {
            return Vector3.Distance(Position, other);
        }

        public PathNode SpawnChildren(float stepLength, float rotationAngle, float timeDelta)
        {
            PathNode result = new PathNode(this);

            //  Вращаем вокруг вертикальной оси, что в принципе не очень хорошо - надо бы более универсально, нормаль к поверхности взять, и всё такое
            result.Direction = Quaternion.AngleAxis(rotationAngle, Vector3.up) * Direction;
            result.Direction.Normalize();

            //  Перемещаемся в новую позицию
            result.Position = Position + result.Direction * stepLength;

            //  Момент времени считаем
            result.TimeMoment = TimeMoment + timeDelta;

            result.OtherRegionLength = OtherRegionLength;

            return result;
        }

        /// <summary>
        /// Преобразует точку в сеточное представление
        /// </summary>
        public (int, int, int, int) ToGrid(float distDelta)
        {
            int posX = Mathf.RoundToInt(Position.x / distDelta);
            int posZ = Mathf.RoundToInt(Position.z / distDelta);
            int dirX = Mathf.RoundToInt(Direction.x / distDelta);
            int dirZ = Mathf.RoundToInt(Direction.z / distDelta);
            return (posX, posZ, dirX, dirZ);
        }

        /// <summary>
        /// Проверяет, находится ли точка на высоте
        /// </summary>
        public bool IsAboveTheGround()
        {
            return Physics.Raycast(Position, new Vector3(0, -1, 0), out RaycastHit _, 5);
        }

        /// <summary>
        /// Проверка того, что точка проходима
        /// </summary>
        /// <returns></returns>
        public bool IsWalkable()
        {
            if (!IsAboveTheGround()) return false;

            var collisions = Physics.OverlapSphere(Position, 1.0f);
            foreach (var col in collisions)
            {
                if (col.CompareTag("Box"))
                    return false;
            }

            return true;
        }
    }
}