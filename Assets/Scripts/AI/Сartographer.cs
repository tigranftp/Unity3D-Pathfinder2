using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// По-хорошему это октодерево должно быть, но неохота.
/// Класс, владеющий полной информацией о сцене - какие области где расположены, 
/// как связаны между собой, и прочая информация.
/// Должен по координатам точки определять номер области.
/// </summary>

namespace BaseAI
{
    /// <summary>
    /// Базовый класс для реализации региона - квадратной или круглой области
    /// </summary>
    public interface IBaseRegion
    {
        /// <summary>
        /// Индекс региона - соответствует индексу элемента в списке регионов
        /// </summary>
        int index { get; set; }

        /// <summary>
        /// Список соседних регионов (в которые можно перейти из этого)
        /// </summary>
        IList<IBaseRegion> Neighbors { get; set; }
        
        /// <summary>
        /// Принадлежит ли точка региону (с учётом времени)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        bool Contains(PathNode node);
        
        /// <summary>
        /// Является ли регион динамическим
        /// </summary>
        bool Dynamic { get; }

        void TransformPoint(PathNode parent, PathNode node);

        /// <summary>
        /// Квадрат расстояния до ближайшей точки региона (без учёта времени)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        float SqrDistanceTo(PathNode node);

        /// <summary>
        /// Добавление времени транзита через регион
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        void AddTransferTime(IBaseRegion source, IBaseRegion dest);

        /// <summary>
        /// Время перехода через область насквозь, от одного до другого 
        /// </summary>
        /// <param name="source">Регион, с границы которого стартуем</param>
        /// <param name="transitStart">Глобальное время начала перехода</param>
        /// <param name="dest">Регион назначения - ближайшая точка</param>
        /// <returns>Глобальное время появления в целевом регионе</returns>
        float TransferTime(IBaseRegion source, float transitStart, IBaseRegion dest);
        
        /// <summary>
        /// Центральная точка региона - используется для марштуризации
        /// </summary>
        /// <returns></returns>
        Vector3 GetCenter();
    }

    /// <summary>
    /// Сферический регион на основе SphereCollider
    /// </summary>
    public class SphereRegion : IBaseRegion
    {
        /// <summary>
        /// Тело региона - коллайдер
        /// </summary>
        public SphereCollider body;
        
        /// <summary>
        /// Расстояние транзита через регион
        /// </summary>
        private Dictionary<System.Tuple<int, int>, string> transits;

        /// <summary>
        /// Индекс региона в списке регионов
        /// </summary>
        public int index { get; set; } = -1;

        bool IBaseRegion.Dynamic { get; } = false;
        void IBaseRegion.TransformPoint(PathNode parent, PathNode node) { return; }

        public IList<IBaseRegion> Neighbors { get; set; } = new List<IBaseRegion>();


        public SphereRegion(SphereCollider sample)
        {
            body = sample;
        }

        /// <summary>
        /// Квадрат расстояния до региона (минимально расстояние до границ коллайдера в квадрате)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public float SqrDistanceTo(PathNode node) { return body.bounds.SqrDistance(node.Position); }
        /// <summary>
        /// Проверка принадлежности точки региону
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool Contains(PathNode node) { return body.bounds.Contains(node.Position); }



        /// <summary>
        /// Время перехода через область насквозь, от одного до другого 
        /// </summary>
        /// <param name="source">Регион, с границы которого стартуем</param>
        /// <param name="transitStart">Глобальное время начала перехода</param>
        /// <param name="dest">Регион назначения - ближайшая точка</param>
        /// <returns>Глобальное время появления в целевом регионе</returns>
        public float TransferTime(IBaseRegion source, float transitStart, IBaseRegion dest) {
            throw new System.NotImplementedException();
        }

        public Vector3 GetCenter() 
        {
            //  Вроде бы должно работать
            return body.bounds.center;
        }

        void IBaseRegion.AddTransferTime(IBaseRegion source, IBaseRegion dest)
        {
            throw new System.NotImplementedException();
        }
    }
    
    /// <summary>
    /// Сферический регион на основе BoxCollider
    /// </summary>
    public class BoxRegion : IBaseRegion
    {
        /// <summary>
        /// Тело коллайдера для представления региона
        /// </summary>
        public BoxCollider body;
        
        /// <summary>
        /// Индекс региона в списке регионов
        /// </summary>
        public int index { get; set; } = -1;
        
        bool IBaseRegion.Dynamic { get; } = false;
        void IBaseRegion.TransformPoint(PathNode parent, PathNode node) { return; }

        public IList<IBaseRegion> Neighbors { get; set; } = new List<IBaseRegion>();
        
        /// <summary>
        /// Создание региона с кубическим коллайдером в качестве основы
        /// </summary>
        /// <param name="RegionIndex"></param>
        /// <param name="position"></param>
        /// <param name="size"></param>
        public BoxRegion(BoxCollider sample)
        {
            body = sample;
        }
        
        /// <summary>
        /// Квадрат расстояния до региона (минимально расстояние до границ коллайдера в квадрате)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public float SqrDistanceTo(PathNode node) { return body.bounds.SqrDistance(node.Position); }
        
        /// <summary>
        /// Проверка принадлежности точки региону
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool Contains(PathNode node) { return body.bounds.Contains(node.Position); }
        
        /// <summary>
        /// Время перехода через область насквозь, от одного до другого 
        /// </summary>
        /// <param name="source">Регион, с границы которого стартуем</param>
        /// <param name="transitStart">Глобальное время начала перехода</param>
        /// <param name="dest">Регион назначения - ближайшая точка</param>
        /// <returns>Глобальное время появления в целевом регионе</returns>
        public float TransferTime(IBaseRegion source, float transitStart, IBaseRegion dest)
        {
            throw new System.NotImplementedException();
        }

        public Vector3 GetCenter()
        {
            //  Вроде бы должно работать
            return body.bounds.center;
        }

        void IBaseRegion.AddTransferTime(IBaseRegion source, IBaseRegion dest)
        {
            throw new System.NotImplementedException();
        }
    }

    public class Cartographer
    {
        //  Список регионов
        public List<IBaseRegion> regions = new List<IBaseRegion>();

        //  Поверхность (Terrain) сцены
        public Terrain SceneTerrain;

        // Start is called before the first frame update
        public Cartographer(GameObject collidersCollection)
        {
            //  Получить Terrain. Пробуем просто найти Terrain на сцене
            try
            {
                SceneTerrain = (Terrain)Object.FindObjectOfType(typeof(Terrain));
            }
            catch (System.Exception e)
            {
                Debug.Log("Can't find Terrain!!!" + e.Message);
            }

            //  Создаём региончики
            //  Они уже созданы в редакторе, как коллекция коллайдеров - повешена на объект игровой сцены CollidersMaster внутри объекта Surface
            //  Их просто нужно вытащить списком, и запихнуть в список регионов.
            //  Но есть проблема - не перепутать индексы регионов! Нам нужно вручную настроить списки смежности - какой регион с
            //  каким граничит. Это можно автоматизировать, как-никак у нас коллайдеры с наложением размещены, но вообще это
            //  не сработает для динамических регионов (коллайдеры которых перемещаются) - они автоматически не установят связи.
            //  Поэтому открываем картинку RegionsMap.png в корне проекта, и ручками дорисовываем регионы, и связи между ними.

            var colliders = collidersCollection.GetComponents<Collider>();
            foreach (var collider in colliders)
            {
                if (collider.GetType() == typeof(BoxCollider)) {
                    regions.Add(new BoxRegion((BoxCollider)collider));
                    regions[regions.Count - 1].index = regions.Count - 1;
                    continue;
                }
                if (collider.GetType() == typeof(SphereCollider))
                {
                    regions.Add(new SphereRegion((SphereCollider)collider));
                    regions[regions.Count - 1].index = regions.Count - 1; 
                    continue;
                }

                throw new System.Exception("You can't add any other types of colliders except of Box and Sphere!");
            }

            //  Настраиваем связи между регионами - не самая лучшая идея, но для крупных регионов сойдёт
            regions[0].Neighbors.Add(regions[1]);
            regions[0].Neighbors.Add(regions[3]);

            regions[1].Neighbors.Add(regions[0]);
            regions[1].Neighbors.Add(regions[2]);

            regions[2].Neighbors.Add(regions[1]);
            regions[2].Neighbors.Add(regions[4]);

            regions[3].Neighbors.Add(regions[0]);

            regions[4].Neighbors.Add(regions[2]);


            //  Платформы потом. Для них реализовать класс "BaseRegion", и его подсовывать в этот список, обновляя 
            //  списки смежности
        }

        /// <summary>
        /// Регион, которому принадлежит точка. Сделать абы как
        /// </summary>
        /// <param name="node"></param>
        /// <returns>Индекс региона, -1 если не принадлежит (не проходима)</returns>
        public IBaseRegion GetRegion(PathNode node)
        {
            for (var i = 0; i < regions.Count; ++i)
                //  Метод полиморфный и для всяких платформ должен быть корректно в них реализован
                if (regions[i].Contains(node))
                    return regions[i];
            Debug.Log("Not found region for " + node.Position.ToString());
            return null;
        }
    }
}