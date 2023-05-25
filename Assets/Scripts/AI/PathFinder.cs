using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using Priority_Queue;

namespace BaseAI
{
    /// <summary>
    /// Делегат для обновления пути - вызывается по завершению построения пути
    /// </summary>
    /// <param name="pathNodes"></param>
    /// <returns>Успешно ли построен путь до цели</returns>
    public delegate void UpdatePathListDelegate(List<PathNode> pathNodes, int? newRegion);

    /// <summary>
    /// Делегат для перехода платформы - вызывается, когда нужно использовать платформу
    /// </summary>
    /// <param name="timeToPlatformJump">Момент времени, когда нужно будет запрыгнуть на платформу</param>
    /// <param name="timeToPlatformLeave">Момент времени, когда нужно будет спрыгнуть с платформы</param>
    public delegate void TransitPlatformDelegate(float timeToPlatformJump, float timeToPlatformLeave);

    /// <summary>
    /// Локальный маршрутизатор - ищет маршруты от локальной точки какого-либо региона до указанного региона
    /// </summary>
    public class LocalPathFinder : MonoBehaviour
    {
        /// <summary>
        /// Картограф - класс, хранящий информацию о геометрии уровня, регионах и прочем
        /// </summary>
        private Cartographer cartographer;

        //// Start is called before the first frame update
        void Awake()
        {
            cartographer = FindObjectOfType<Cartographer>();
        }

        /// <summary>
        /// Проверяет, перешагнули ли мы цель за последний шаг
        /// </summary>
        private bool IsCrossTarget(PathNode newNode, PathNode prevNode, PathNode target, MovementProperties properties)
        {
            float step = properties.deltaTime * properties.maxSpeed;
            var direction = prevNode.Position - newNode.Position;
            var collisions = Physics.RaycastAll(prevNode.Position, direction, step);
            foreach (var col in collisions)
            {
                if (col.transform.position == target.Position)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Получить список соседних точек
        /// </summary>
        /// <returns></returns>
        List<PathNode> GetNeighbours(PathNode node, MovementProperties properties)
        {
            float step = properties.deltaTime * properties.maxSpeed;
            List<PathNode> result = new List<PathNode>();

            //  Внешний цикл отвечает за длину шага - либо 0 (остаёмся в точке), либо 1 - шагаем вперёд
            for (int mult = 0; mult <= 1; ++mult)
                //  Внутренний цикл перебирает углы поворота
                for (int angleStep = -properties.angleSteps; angleStep <= properties.angleSteps; ++angleStep)
                {
                    PathNode next = node.SpawnChildren(step * mult, angleStep * properties.rotationAngle, properties.deltaTime);
                    if (next.IsWalkable())
                    {
                        Debug.DrawLine(node.Position, next.Position, Color.blue, 10f);
                        result.Add(next);
                    }
                }
            return result;
        }

        /// <summary>
        /// Эвристика для локального планировщика
        /// </summary>
        float Hieristic(PathNode current, PathNode finish, MovementProperties properties)
        {
            float dist = Vector3.Distance(current.Position, finish.Position);
            return dist / properties.maxSpeed;
        }

        /// <summary>
        /// Построение маршрута от заданной точки через один из заданных порталов
        /// </summary>
        /// <param name="start">Начальная точка для поиска</param>
        /// <param name="portalNames">Имена порталов, до которых нужно дойти</param>
        /// <param name="properties">Параметры движения</param>
        /// <returns>Список точек маршрута</returns>
        public List<PathNode> FindPath(PathNode start, string[] portalNames, MovementProperties properties)
        {
            var portal = cartographer.portalsRegions.Find(
                obj => portalNames.Any(portalName => obj.name == portalName)
            );
            if (portal == null) return null;

            var finishPathNode = new PathNode(portal.transform.position, Vector3.zero);
            return FindPath(start, finishPathNode, properties);
        }

        /// <summary>
        /// Локальный путь между двумя точками
        /// </summary>
        public List<PathNode> FindPath(PathNode start, PathNode finish, MovementProperties properties)
        {
            var startRegion = cartographer.GetBoxRegionByPathNode(start); // сохраняем стартовый регион

            Debug.Log($"Запускаем построение локального пути {start.Position} -> {finish.Position} (регион {startRegion.gameObject.name})");
            Debug.Log($"Старт {start.Position} направление {start.Direction}, финиш {finish.Position}");

            // если уже почти пришли, возвращаем список из одной финальной позиции
            if (Vector3.Distance(start.Position, finish.Position) < properties.epsilon)
                return new List<PathNode>() {
                    new PathNode(start.Position, start.Direction) { Parent = start }
                };

            var opened = new SimplePriorityQueue<PathNode>();
            var closed = new SortedSet<(int, int, int, int)>(); // берем только x и z координаты

            start.F = 0;
            opened.Enqueue(start, 0);
            closed.Add(start.ToGrid(properties.deltaDist));

            PathNode lastPathNode = null;

            int counter = 0;
            while (opened.Count > 0 && ++counter < 1000)
            {
                PathNode current = opened.Dequeue();

                // если это уже третья точка, выходящая в другой регион, обрубаем путь
                if (current.OtherRegionLength > 2) continue;

                // если осталось шаг или менее- явно идем туда
                if (Vector3.Distance(current.Position, finish.Position) < properties.maxSpeed)
                {
                    lastPathNode = new PathNode(finish.Position, finish.Direction);
                    lastPathNode.Parent = current.Parent;
                    break;
                }
                // если уже перешагнули цель, идем прямо в цель (длина шага уменьшается)
                if (current.Parent != null && IsCrossTarget(current, current.Parent, finish, properties))
                {
                    lastPathNode = new PathNode(finish.Position, finish.Direction);
                    lastPathNode.Parent = current.Parent;
                    break;
                }

                foreach (var next in GetNeighbours(current, properties))
                {
                    var gridNode = next.ToGrid(properties.deltaDist);
                    if (!closed.Contains(gridNode))
                    {
                        float f = next.F + Hieristic(next, finish, properties);
                        next.F = f;

                        if (!startRegion.Contains(next)) next.OtherRegionLength++;

                        next.Parent = current;
                        opened.Enqueue(next, f);
                        closed.Add(gridNode);
                    }
                }
            }

            Debug.Log($"Рассмотрено {counter} узлов");
            
            if (lastPathNode == null) // не удалось построить маршрут
            {
                Debug.LogWarning("Не удалось построить локальный маршрут");
                return null;
            }

            var result = new List<PathNode>();
            while (lastPathNode != null)
            {
                result.Add(lastPathNode);

                if (lastPathNode.Parent != null)
                    Debug.DrawLine(lastPathNode.Position, lastPathNode.Parent.Position, Color.red, 10f);

                lastPathNode = lastPathNode.Parent;
            }

            result.Reverse();
            return result;
        }
    }

    /// <summary>
    /// Глобальный маршрутизатор
    /// </summary>
    public class PathFinder : MonoBehaviour
    {
        /// <summary>
        /// Картограф - класс, хранящий информацию о геометрии уровня, регионах и прочем
        /// </summary>
        private Cartographer cartographer;

        /// <summary>
        /// Локальный планировщик
        /// </summary>
        private LocalPathFinder localPathFinder;

        //// Start is called before the first frame update
        void Awake()
        {
            cartographer = FindObjectOfType<Cartographer>();

            localPathFinder = FindObjectOfType<LocalPathFinder>();
            if (localPathFinder == null)
                localPathFinder = gameObject.AddComponent<LocalPathFinder>();
        }

        /// <summary>
        /// Эвристика для регионов глобального планировщика
        /// </summary>
        float Hieristic(BoxRegion curRegion, BoxRegion finishRegion, float curTime)
        {
            return curRegion.TransferTime(curTime, finishRegion);
        }

        /// <summary>
        /// Найти список номеров регионов, через которые нужно идти
        /// </summary>
        /// <param name="start">Начальный регион</param>
        /// <param name="finish">Финальный регион</param>
        /// <returns>Список индексов регионов</returns>
        List<int> FindGlobalPath(BoxRegion start, BoxRegion finish)
        {
            var queue = new SimplePriorityQueue<BoxRegion>();
            var closed = new HashSet<int>();
            var curTime = Time.fixedTime;

            float start_f = Hieristic(start, finish, curTime);
            start.f = start_f;
            queue.Enqueue(start, 0);
            closed.Add(start.index);
            
            while (queue.Count > 0)
            {
                BoxRegion curRegion = queue.Dequeue();

                if (curRegion.index == finish.index)
                {
                    var ans = new List<int>();
                    BaseRegion ansPathNode = finish;

                    while (ansPathNode != null)
                    {
                        ans.Add(ansPathNode.index);
                        ansPathNode = ansPathNode.Parent;
                    }
                    ans.Reverse();
                    return ans;
                }

                closed.Add(curRegion.index);
                foreach (var nei in curRegion.Neighbors)
                {
                    var neighbor = nei as BoxRegion;
                    float f = curRegion.f + curRegion.TransferTime(curTime, neighbor) +
                        Hieristic(neighbor, finish, curTime);

                    if (neighbor.f > f && !closed.Contains(neighbor.index))
                    {
                        neighbor.f = f;
                        neighbor.Parent = curRegion;
                        queue.Enqueue(neighbor, f);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Подсчитать время, в которое нужно запрыгнуть и спрыгнуть с платформы, и передать их в делегат
        /// </summary>
        /// <param name="regionIndex">Индекс региона, в который нужно попасть через платформу</param>
        /// <param name="playerPosition">Позиция персонажа</param>
        /// <param name="platformTransiter">Делегат, в который передается время</param>
        void TransitPlatform(int regionIndex, PathNode playerPosition, TransitPlatformDelegate platformTransiter)
        {
            var (timeToJump, timeToLeave) = cartographer.GetTimesToPlatformTransition(regionIndex, playerPosition, Time.fixedTime);
            platformTransiter(timeToJump, timeToLeave);
        }

        private int? platformTransitionFrom = null;
        private PathNode waitingPlatformPosition = null;

        /// <summary>
        /// Собственно метод построения пути
        /// </summary>
        void PathfindingTask(PathNode start, PathNode finish, MovementProperties movementProperties, 
            UpdatePathListDelegate updater, TransitPlatformDelegate platformTransiter)
        {
            // если сейчас нужно перейти платформу
            if (platformTransitionFrom.HasValue)
            {
                TransitPlatform(platformTransitionFrom.Value, waitingPlatformPosition, platformTransiter);
                updater(new List<PathNode>(), null);
                platformTransitionFrom = null;
                return;
            }

            Debug.Log($"Запускаем путь {start.Position} -> {finish.Position}");

            BoxRegion startRegion = cartographer.GetBoxRegionByPathNode(start);
            BoxRegion finishRegion = cartographer.GetBoxRegionByPathNode(finish);

            if (startRegion == null)
            {
                Debug.LogWarning("Не удалось найти регион начальной точки");
                return;
            }
            else if (finishRegion == null)
            {
                Debug.LogWarning("Не удалось найти регион целевой точки");
                return;
            }

            Debug.Log($"Запрошен глобальный путь из секторов {startRegion.index + 1} -> {finishRegion.index + 1}");

            cartographer.regions.ForEach(region =>
            {
                region.f = float.PositiveInfinity;
                region.Parent = null;
            });
            var regionInds = FindGlobalPath(startRegion, finishRegion);
            if (regionInds == null)
            {
                Debug.LogWarning("Не удалось построить глобальный путь");
                return;
            }

            string globalPath = string.Join(" -> ", regionInds.Select(id => id + 1));
            Debug.Log($"Построен глобальный маршрут по регионам: {globalPath}");

            if (regionInds.Count > 1)
            {
                var path = localPathFinder.FindPath(
                    start,
                    new[] {
                        $"Portal_{regionInds[0] + 1}_{regionInds[1] + 1}",
                        $"Portal_{regionInds[1] + 1}_{regionInds[0] + 1}"
                    },
                    movementProperties
                );
                if (path != null) // успешно построили до следующего региона
                {
                    var lastPathNode = new PathNode(path[path.Count - 1].Position, path[path.Count - 1].Direction);
                    lastPathNode.Parent = path[path.Count - 1];
                    path.Add(lastPathNode);
                }
                updater(path, regionInds[1]);

                // если регион, в который пришли, находится перед платформой, ставим флажок
                // это значит, что при следующем глобальном планировании надо переправляться
                if (cartographer.IsPlatformBetweenRegions(regionInds[0], regionInds[1]))
                {
                    platformTransitionFrom = regionInds[1];
                    if (path != null) waitingPlatformPosition = path[path.Count - 1];
                }
                else
                    platformTransitionFrom = null; // иначе сбрасываем флажок
            }
            else
            {
                updater(localPathFinder.FindPath(start, finish, movementProperties), null);
                platformTransitionFrom = null; // сбрасываем флажок
            }
        }

        public bool FindPath(PathNode start, PathNode finish, MovementProperties movementProperties,
            UpdatePathListDelegate updater, TransitPlatformDelegate platformTransiter)
        {
           
            PathfindingTask(start, finish, movementProperties, updater, platformTransiter);
            return true;
        }
    }
}