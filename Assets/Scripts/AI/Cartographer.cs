using BaseAI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// По-хорошему это октодерево должно быть, но неохота.
/// Класс, владеющий полной информацией о сцене - какие области где расположены, 
/// как связаны между собой, и прочая информация.
/// Должен по координатам точки определять номер области.
/// </summary>

public class Cartographer : MonoBehaviour
{
    [SerializeField] List<GameObject> mapRegions;
    public List<GameObject> platformsRegions;
    public List<GameObject> portalsRegions;
    public GameObject finishPoint;

    //  Список регионов
    public List<BaseRegion> regions;

    void Awake()
    {
        regions = new List<BaseRegion>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // Формируем список регионов
        int i = 0;
        foreach (var region in mapRegions)
        {
            regions.Add(new BoxRegion(i++, region));
        }

        foreach (var region in portalsRegions)
        {
            regions.Add(new SphereRegion(i++, region));
            var regionIndexes = region.name.Split('_').Skip(1).Select(s => int.Parse(s)-1).ToArray();
            regions[regionIndexes[0]].Neighbors.Add(regions[regionIndexes[1]]);
            regions[regionIndexes[1]].Neighbors.Add(regions[regionIndexes[0]]);
        }

        foreach (var region in platformsRegions)
        {
            var regionIndexes = region.name.Split('_').Skip(1).Select(s => int.Parse(s)-1).ToArray();

            var from = regions.Find(reg => reg is BoxRegion && reg.index == regionIndexes[0]);
            var to = regions.Find(reg => reg is BoxRegion && reg.index == regionIndexes[1]);
            regions.Add(new PlatformRegion(i++, region, from as BoxRegion, to as BoxRegion));

            regions[regionIndexes[0]].Neighbors.Add(regions[regionIndexes[1]]);
            regions[regionIndexes[1]].Neighbors.Add(regions[regionIndexes[0]]);
        }
    }

    /// <summary>
    /// Получить квадратный регион (сегмент), которому принадлежит PathNode
    /// </summary>
    /// <returns>Квадратный регион или null, если регион не найден (точка не проходима)</returns>
    public BoxRegion GetBoxRegionByPathNode(PathNode node)
    {
        // Если у PathNode явно задан регион, просто возвращаем его
        if (node.RegionIndex.HasValue)
        {
            return GetBoxRegionByIndex(node.RegionIndex.Value);
        }

        // иначе ищем регион
        var region = regions.Find(reg => reg is BoxRegion && reg.Contains(node));
        return region != null ? region as BoxRegion : null;
    }

    /// <summary>
    /// Получить квадратный регион (сегмент) по его индексу
    /// </summary>
    /// <returns>Квадратный регион или null, если регион не найден</returns>
    public BoxRegion GetBoxRegionByIndex(int index)
    {
        return (index >= 0 && index < regions.Count) ? regions[index] as BoxRegion : null;
    }

    /// <summary>
    /// Проверяет, что между двумя регионами есть платформа
    /// </summary>
    /// <param name="fromIndex"></param>
    /// <param name="toIndex"></param>
    /// <returns></returns>
    public bool IsPlatformBetweenRegions(int fromIndex, int toIndex)
    {
        return platformsRegions.Any(platformRegion => platformRegion.name == $"Platform_{fromIndex + 1}_{toIndex + 1}");
    }

    /// <summary>
    /// Подсчитать время, когда нужно запрыгнуть на платформу и спрыгнуть с нее
    /// </summary>
    /// <param name="regionIndex">Индекс региона, в который нужно попасть через платформу</param>
    /// <param name="playerPosition">Позиция персонажа</param>
    /// <param name="timeMoment">Текущий момент времени</param>
    /// <returns></returns>
    public (float, float) GetTimesToPlatformTransition(int regionIndex, PathNode playerPosition, float timeMoment)
    {
        var platform = platformsRegions.Find(
            obj => obj.name.EndsWith($"_{regionIndex + 1}")
        );
        if (platform == null)
            throw new ArgumentException("Не найдена платформа для заданного конечного региона");

        var platformRegion = regions.Find(region => region.gameObject == platform) as PlatformRegion;

        // вычисляем время переправы относительно первой платформы (лин. зависимость от скорости)
        float transitionTime = 5f * (40f / platformRegion.Speed);

        const float rotationTimeStep = 0.02f; // шаг для проверки расстояния
        float timeToBoard = rotationTimeStep;
        float minDistance = float.MaxValue, curTime = rotationTimeStep;

        // вычислим, когда прыгать для посадки, будем крутить точку по окружности
        PathNode startPoint = new PathNode(playerPosition.Position, Vector3.zero);
        while (curTime <= transitionTime * 2) // перебираем время до полного оборота на 360 град
        {
            PathNode rotatedPoint = platformRegion.GetRotatedPoint(startPoint, curTime);
            
            float distance = rotatedPoint.Distance(platform.transform.position);
            // хочу, чтобы бот стоял в ожидании минимум полсекунды (если платформа придет быстрее - пропустим)
            if (curTime > 0.5f && minDistance > distance)
            {
                minDistance = distance;
                timeToBoard = curTime;
            }
            curTime += rotationTimeStep;
        }

        timeToBoard -= 1f; // поправка на то, что нужно начать прыгать чуть раньше (разгон и тд0

        return (timeMoment + timeToBoard, timeMoment + timeToBoard + transitionTime);
    }
}
