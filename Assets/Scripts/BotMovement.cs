using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotMovement : MonoBehaviour
{
    /// <summary>
    /// Ссылка на глобальный планировщик - в целом, он-то нам и занимается построением пути
    /// </summary>
    private BaseAI.PathFinder GlobalPathfinder;

    /// <summary>
    /// Запланированный путь как список точек маршрута
    /// </summary>
    public List<BaseAI.PathNode> plannedPath;

    /// <summary>
    /// Текущий путь как список точек маршрута
    /// </summary>
    [SerializeField] List<BaseAI.PathNode> currentPath = null;

    /// <summary>
    /// Текущая целевая точка - цель нашего движения. Обновления пока что не предусмотрено
    /// </summary>
    private BaseAI.PathNode currentTarget = null;

    /// <summary>
    /// Параметры движения бота
    /// </summary>
    [SerializeField] private BaseAI.MovementProperties movementProperties;

    /// <summary>
    /// Регион, в котором находится бот (меняется извне при смене региона)
    /// </summary>
    private int? CurrentRegionIndex;

    /// <summary>
    /// Целевая точка для движения - глобальная цель
    /// </summary>
    [SerializeField] private Vector3 FinishPointVector3;  //  Конечная цель маршрута как Vector3
    private BaseAI.PathNode FinishPoint;                  //  Конечная цель маршрута как PathNode - вот оно нафига вообще?
    const int MinimumPathNodesLeft = 10;                  //  Минимальное число оставшихся точек в маршруте, при котором вызывается перестроение

    /// <summary>
    /// Было ли запрошено обновление пути. Оно в отдельном потоке выполняется, поэтому если пути нет, но 
    /// запрос планировщику подали, то надо просто ждать. В тяжелых случаях можно сделать отметку времени - когда был 
    /// сделан запрос, и по прошествию слишком большого времени выбрасывать исключение.
    /// </summary>
    private bool pathUpdateRequested = false;

    public float obstacleRange = 5.0f;
    public int steps = 0;
    private float leftLegAngle = 3f;  //  Угол левой ноги - только для анимации движения используется
    private Action jumpAction = null;

    /// <summary>
    /// Находимся ли в полёте (в состоянии прыжка)
    /// </summary>
    private bool isJumpimg;

    /// <summary>
    /// Является ли платформа следующим "регионом"
    /// </summary>
    private bool isPlatformNext;

    /// <summary>
    /// Стоим ли мы сейчас на платформе
    /// </summary>
    private bool isInPlatform = false;

    /// <summary>
    /// На сколько градусов еще нужно развернуться?
    /// </summary>
    private float needToRotate = 180f;

    private float timeToPlatformJump = -1;
    private float timeToPlatformLeave = -1;

    /// <summary>
    /// Заглушка - двигается ли бот или нет
    /// </summary>
    [SerializeField] private bool walking = false;

    //  Сила, тянущая "вверх" упавшего бота и заставляющая его вставать
    [SerializeField] float force = 5.0f;
    //  Угол отклонения, при котором начинает действовать "поднимающая" бота сила
    [SerializeField] float max_angle = 20.0f;

    [SerializeField] private GameObject leftLeg = null;
    [SerializeField] private GameObject rightLeg = null;
    [SerializeField] private GameObject leftLegJoint = null;
    [SerializeField] private GameObject rightLegJoint = null;

    void Start()
    {
        //  Ищем глобальный планировщик на сцене
        GlobalPathfinder = (BaseAI.PathFinder)FindObjectOfType(typeof(BaseAI.PathFinder));
        if (GlobalPathfinder == null)
        {
            Debug.Log("Не могу найти глобальный планировщик!");
            throw new ArgumentNullException("Can't find global pathfinder!");
        }

        Cartographer cartographer = FindObjectOfType<Cartographer>();

        FinishPointVector3 = cartographer.finishPoint.gameObject.transform.localPosition;
        FinishPoint = new BaseAI.PathNode(FinishPointVector3, Vector3.zero);
    }

    /// <summary>
    /// Движение ног
    /// </summary>
    void MoveLegs()
    {
        
        //  Движение ножек сделать
        if (steps >= 20)
        {
            leftLegAngle = -leftLegAngle;
            steps = -20;
        }
        steps++;

        leftLeg.transform.RotateAround(leftLegJoint.transform.position, transform.right, leftLegAngle);
        rightLeg.transform.RotateAround(rightLegJoint.transform.position, transform.right, -leftLegAngle);
    }

    /// <summary>
    /// Делегат, выполняющийся при построении пути планировщиком
    /// </summary>
    /// <param name="pathNodes"></param>
    public void UpdatePathListDelegate(List<BaseAI.PathNode> pathNodes, int? newRegion)
    {
        if (pathUpdateRequested == false)
        {
            //  Пока мы там путь строили, уже и не надо стало - выключили запрос
            return;
        }
        //  Просто перекидываем список, и всё
        plannedPath = pathNodes;
        pathUpdateRequested = false;
        CurrentRegionIndex = newRegion.HasValue ? (int?)newRegion.Value : null;
    }

    /// <summary>
    /// Делегат получения времени прохлждения платформы. Просто сохраняет время в переменных
    /// </summary>
    /// <param name="timeToPlatformJump">Время, когда нужно запрыгнуть на платформу</param>
    /// <param name="timeToPlatformLeave">Время, когда нужно спрыгнуть с платформы</param>
    public void TransitPlatform(float timeToPlatformJump, float timeToPlatformLeave)
    {
        isPlatformNext = true;
        Debug.LogWarning($"Нужно прыгнуть на платформу в {timeToPlatformJump}, спрыгнуть в {timeToPlatformLeave}");
        this.timeToPlatformJump = timeToPlatformJump;
        this.timeToPlatformLeave = timeToPlatformLeave;
    }

    /// <summary>
    /// Запрос на достроение пути.
    /// </summary>
    private bool RequestPathfinder()
    {
        if (FinishPoint == null || pathUpdateRequested || plannedPath != null) return false;
        
        if (Vector3.Distance(transform.position, FinishPoint.Position) < movementProperties.epsilon * 2f)
        {
            //  Всё, до цели дошли, сушите вёсла
            FinishPoint = null;
            FinishPointVector3 = transform.position;
            plannedPath = null;
            currentPath = null;
            pathUpdateRequested = false;
            Debug.Log("Дошли до финиша!");
            return false;
        }

        //  Тут два варианта - либо запускаем построение пути от хвоста списка, либо от текущей точки
        BaseAI.PathNode startOfRoute = null;
        if (currentPath != null && currentPath.Count > 0)
            startOfRoute = currentPath[currentPath.Count - 1];
        else
            //  Из начального положения начнём - вот только со временем беда. Технически надо бы брать момент в будущем, когда 
            //  начнём движение, но мы не знаем когда маршрут построится. Надеемся, что быстро
            startOfRoute = new BaseAI.PathNode(transform.position, transform.forward);

        if (CurrentRegionIndex.HasValue)
        {
            startOfRoute.RegionIndex = CurrentRegionIndex.Value;
        }
        pathUpdateRequested = true;
        Debug.Log("Запрошено построение маршрута");
        GlobalPathfinder.FindPath(startOfRoute, FinishPoint, movementProperties, UpdatePathListDelegate, TransitPlatform);

        return true;
    }

    /// <summary>
    /// Обновление текущей целевой точки - куда вообще двигаться
    /// </summary>
    private bool UpdateCurrentTargetPoint()
    {
        //  Если есть текущая целевая точка
        if (currentTarget != null)
        {
            float distanceToTarget = currentTarget.Distance(transform.position);
            //  Если до текущей целевой точки ещё далеко, то выходим
            if (distanceToTarget >= movementProperties.epsilon || currentTarget.TimeMoment - Time.fixedTime > movementProperties.epsilon) return true;
            //  Иначе удаляем её из маршрута и берём следующую
            currentPath.RemoveAt(0);
            if (currentPath.Count > 0)
            {
                //  Берём очередную точку и на выход
                currentTarget = currentPath[0];
                return true;
            }
            else
            {
                currentTarget = null;
                currentPath = null;
            }
        }
        else
        if (currentPath != null)
        {
            if (currentPath.Count > 0)
            {
                currentTarget = currentPath[0];
                return true;
            }
            else
            {
                currentPath = null;
            }
        }

        //  Здесь мы только в том случае, если целевой нет, и текущего пути нет - и то, и другое null
        //  Обращение к plannedPath желательно сделать через блокировку - именно этот список задаётся извне планировщиком
        //  Непонятно, насколько lock затратен, можно ещё булевский флажок добавить, его сначала проверять
        //  Но сначала сделаем всё на "авось", без блокировок - там же просто ссылка на список переприсваевается.

        if (plannedPath != null)
        {
            currentPath = plannedPath;
            plannedPath = null;
            if (currentPath.Count > 0)
                currentTarget = currentPath[0];
        }

        return currentTarget != null;
    }

    /// <summary>
    /// Событие, возникающее когда бот касается какого-либо препятствия, то есть приземляется на землю
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionEnter(Collision collision)
    {
        //  Столкнулись - значит, приземлились
        //  Возможно, надо разделить - Terrain и препятствия разнести по разным слоям
        if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacles"))
        {
            var rb = GetComponent<Rigidbody>();
            //  Сбрасываем скорость перед прыжком
            rb.velocity = Vector3.zero;
            isJumpimg = false;

            // если нужно выполнить действие после окончания прыжка - выполняем
            if (jumpAction != null)
            {
                jumpAction();
                jumpAction = null;
            }
        }
    }

    /// <summary>
    /// В зависимости от того, находится ли бот в прыжке, или нет, изменяем цвет ножек
    /// </summary>
    /// <returns></returns>
    bool CheckJumping()
    {
        if (isJumpimg)
        {
            var a = leftLeg.GetComponent<MeshRenderer>();
            a.material.color = Color.red;
            a = rightLeg.GetComponent<MeshRenderer>();
            a.material.color = Color.red;
            return true;
        }
        else
        {
            var a = leftLeg.GetComponent<MeshRenderer>();
            a.material.color = Color.white;
            a = rightLeg.GetComponent<MeshRenderer>();
            a.material.color = Color.white;
            return false;
        }
    }

    /// <summary>
    /// Пытаемся прыгнуть вперёд и вверх (на месте не умеем прыгать)
    /// </summary>
    /// <returns></returns>
    void TryToJump(Action callback = null)
    {
        if (isJumpimg == true) return;
        Debug.Log("ПРЫГАЕМ");

        var rb = GetComponent<Rigidbody>();
        //  Сбрасываем скорость перед прыжком
        rb.velocity = Vector3.zero;
        var jump = transform.forward + 2 * transform.up;
        float jumpForce = movementProperties.jumpForce;
        rb.AddForce(jump * jumpForce, ForceMode.Impulse);
        isJumpimg = true;
        jumpAction = callback;
    }

    /// <summary>
    /// Очередной шаг бота - движение
    /// </summary>
    /// <returns>false, если требуется обновление отображения ножек</returns>
    bool MoveBot()
    {
        // нужно прыгнуть, но мы еще не повернуты в сторону прыжка
        if (isPlatformNext && transform.forward != Vector3.forward)
        {
            float rotateAngle = Vector3.SignedAngle(transform.forward, Vector3.forward, Vector3.up);
            rotateAngle = Mathf.Clamp(rotateAngle, -movementProperties.rotationAngle, movementProperties.rotationAngle);
            transform.Rotate(Vector3.up, rotateAngle);
            return true;
        }
        // время прыгать на платформу и мы стоим лицом вперед
        if (isPlatformNext && Mathf.Abs(Time.fixedTime - timeToPlatformJump) < 0.1f)
        {
            isPlatformNext = false;
            isInPlatform = true;
            needToRotate = 180f;
            TryToJump();
            return true;
        }

        // время прыгать с платформы
        if (isInPlatform && Mathf.Abs(Time.fixedTime - timeToPlatformLeave) < 0.1f)
        {
            if (needToRotate > 0) Debug.LogWarning("Робот не успел развернуться");
            TryToJump(() =>
            {
                isInPlatform = false;
            });
            return true;
        }

        // стоим на платформе, прыгать еще не надо
        if (isInPlatform)
        {
            // за период вращения нужно повернуться на 180 градусов
            if (needToRotate > 0)
            {
                float angleToRotate = Mathf.Min(needToRotate, movementProperties.rotationAngle * 0.5f);
                transform.Rotate(Vector3.up, angleToRotate);
                needToRotate -= angleToRotate;
            }
            return true;
        }

        //  Выполняем обновление текущей целевой точки
        if (!UpdateCurrentTargetPoint())
        {
            //  Это ситуация когда идти некуда - цели нет
            if (!isPlatformNext) RequestPathfinder();
            return false;
        }

        if (CheckJumping()) return true;

        //  Ну у нас тут точно есть целевая точка, вот в неё и пойдём
        //  Определяем угол поворота, и расстояние до целевой
        Vector3 directionToTarget = currentTarget.Position - transform.position;
        float angle = Vector3.SignedAngle(transform.forward, directionToTarget, Vector3.up);
        //  Теперь угол надо привести к допустимому диапазону
        angle = Mathf.Clamp(angle, -movementProperties.rotationAngle, movementProperties.rotationAngle);

        //  Зная угол, нужно получить направление движения (мы можем сразу не повернуть в сторону цели)
        //  Выполняем вращение вокруг оси Oy

        //  Угол определили, теперь, собственно, определяем расстояние для шага
        float stepLength = directionToTarget.magnitude;
        float actualStep = Mathf.Clamp(stepLength, 0.0f, movementProperties.maxSpeed * Time.deltaTime);
        //  Поворот может быть проблемой, если слишком близко подошли к целевой точке
        //  Надо как-то следить за скоростью, она не может превышать расстояние до целевой точки???
        transform.Rotate(Vector3.up, angle);
        transform.position = transform.position + actualStep * transform.forward;
        return true;
    }

    /// <summary>
    /// Вызывается каждый кадр
    /// </summary>
    void Update()
    {
        //  Фрагмент кода, отвечающий за вставание
      //  var vertical_angle = Vector3.Angle(Vector3.up, transform.up);
      //  if (vertical_angle > max_angle)
     //   {
      //      GetComponent<Rigidbody>().AddForceAtPosition(5 * force * Vector3.up, transform.position + 3.0f * transform.up, ForceMode.Force);
       // };

        if (!walking) return;

        //  Собственно движение
        if (MoveBot())
        {
            MoveLegs();
        };
    }
}
