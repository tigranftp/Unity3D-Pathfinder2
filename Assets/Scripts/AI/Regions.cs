using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BaseAI
{
    /// <summary>
    /// Базовый класс для реализации региона - квадратной или круглой области
    /// </summary>
    abstract public class BaseRegion
    {
        /// <summary>
        /// Индекс региона - соответствует индексу элемента в списке регионов
        /// </summary>
        public int index = -1;

        public GameObject gameObject;

        /// <summary>
        /// Список соседних регионов (в которые можно перейти из этого)
        /// </summary>
        public List<BaseRegion> Neighbors { get; set; } = new List<BaseRegion>();

        public float f;

        public BaseRegion Parent;

        protected readonly MovementProperties movementProperties = new MovementProperties();

        /// <summary>
        /// Принадлежит ли точка региону (с учётом времени)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        abstract public bool Contains(PathNode node);

        /// <summary>
        /// Квадрат расстояния до ближайшей точки региона (без учёта времени)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        abstract public float SqrDistanceTo(PathNode node);

        /// <summary>
        /// Время перехода через область насквозь, от одного до другого 
        /// </summary>
        /// <param name="transitStart">Глобальное время начала перехода</param>
        /// <param name="dest">Регион назначения - ближайшая точка</param>
        /// <returns>Глобальное время появления в целевом регионе</returns>
        abstract public float TransferTime(float transitStart, BaseRegion dest);
    }

    /// <summary>
    /// Сферический регион
    /// </summary>
    public class SphereRegion : BaseRegion
    {
        /// <summary>
        /// Тело региона - коллайдер
        /// </summary>
        public Collider body;

        public SphereRegion(int RegionIndex, GameObject plane)
        {
            body = plane.GetComponent<Collider>();
            index = RegionIndex;
        }

        /// <summary>
        /// Квадрат расстояния до региона (минимально расстояние до границ коллайдера в квадрате)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        override public float SqrDistanceTo(PathNode node) { return body.bounds.SqrDistance(node.Position); }
        /// <summary>
        /// Проверка принадлежности точки региону
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        override public bool Contains(PathNode node) { return body.bounds.Contains(node.Position); }

        /// <summary>
        /// Время перехода через область насквозь, от одного до другого 
        /// </summary>
        /// <param name="transitStart">Глобальное время начала перехода</param>
        /// <param name="dest">Регион назначения - ближайшая точка</param>
        /// <returns>Глобальное время появления в целевом регионе</returns>
        override public float TransferTime(float transitStart, BaseRegion dest)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Кубический регион
    /// </summary>
    public class BoxRegion : BaseRegion
    {
        /// <summary>
        /// Тело коллайдера для представления региона
        /// </summary>
        public Collider body;

        /// <summary>
        /// Создание региона с кубическим коллайдером в качестве основы
        /// </summary>
        /// <param name="RegionIndex"></param>
        /// <param name="position"></param>
        /// <param name="size"></param>
        public BoxRegion(int RegionIndex, GameObject plane)
        {
            gameObject = plane;
            body = plane.GetComponent<Collider>();
            index = RegionIndex;
        }

        /// <summary>
        /// Квадрат расстояния до региона (минимально расстояние до границ коллайдера в квадрате)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        override public float SqrDistanceTo(PathNode node) { return body.bounds.SqrDistance(node.Position); }

        /// <summary>
        /// Проверка принадлежности точки региону
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        override public bool Contains(PathNode node) { return body.bounds.Contains(node.Position); }

        /// <summary>
        /// Время перехода через область насквозь, от одного до другого 
        /// </summary>
        /// <param name="transitStart">Глобальное время начала перехода</param>
        /// <param name="dest">Регион назначения - ближайшая точка</param>
        /// <returns>Глобальное время появления в целевом регионе</returns>
        override public float TransferTime(float transitStart, BaseRegion dest)
        {
            // var transform = gameObject.transform as RectTransform;
            // float diagLength = Mathf.Sqrt(Mathf.Pow(transform.rect.width, 2) + Mathf.Pow(transform.rect.height, 2));

            Vector3 sourceCenter = gameObject.transform.position;
            Vector3 destCenter = dest.gameObject.transform.position;

            // todo: если от source до dest есть платформа, считать время как время от центра до портала
            // + время переправы

            return Vector3.Distance(sourceCenter, destCenter) / movementProperties.maxSpeed;
        }
    }

    public class PlatformRegion : BoxRegion
    {
        public BoxRegion from, to;

        private readonly Platform1Movement platformMovement;

        public float Speed { get => platformMovement.rotationSpeed; }
        public float Raduis { get => platformMovement.rotationRadius; }

        public PlatformRegion(int regionIndex, GameObject plane, BoxRegion from, BoxRegion to)
            : base(regionIndex, plane)
        {
            this.from = from;
            this.to = to;
            platformMovement = plane.GetComponentInParent<Platform1Movement>();
        }

        /// <summary>
        /// Получить точку, повернутую по оси вращения платформы (в направлении обратном вращению платформы)
        /// </summary>
        /// <param name="node">Исходная точка</param>
        /// <param name="timeDelta">Время вращения</param>
        /// <returns>Повернутая точка</returns>
        public PathNode GetRotatedPoint(PathNode node, float timeDelta)
        {
            var rotationCenter = platformMovement.rotationCenter;
            var rotationSpeed = platformMovement.rotationSpeed;
            Vector3 dir = node.Position - rotationCenter;
            return new PathNode()
            {
                Position = rotationCenter + Quaternion.AngleAxis(-rotationSpeed * timeDelta, Vector3.up) * dir,
                Direction = Quaternion.AngleAxis(-rotationSpeed * timeDelta, Vector3.up) * node.Direction
            };
        }

        /// <summary>
        /// Время перехода через область насквозь, от одного до другого 
        /// </summary>
        /// <param name="transitStart">Глобальное время начала перехода</param>
        /// <param name="dest">Регион назначения - ближайшая точка</param>
        /// <returns>Глобальное время появления в целевом регионе</returns>
        override public float TransferTime(float transitStart, BaseRegion dest)
        {
            throw new NotImplementedException();
        }
    }
}
