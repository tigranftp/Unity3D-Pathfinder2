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
    /// Целевая точка для движения - глобальная цель
    /// </summary>
    [SerializeField] private GameObject finish;           //  Конечная цель маршрута как Vector3
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

    /// <summary>
    /// Находимся ли в полёте (в состоянии прыжка)
    /// </summary>
    private bool isJumpimg;

    /// <summary>
    /// Время предыдущего обращения к планировщику - не более одного раза в три секунды
    /// </summary>
    private float lastPathfinderRequest;
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
        //  Ищем глобальный планировщик на сцене - абсолютно дурацкий подход, но так можно
        //  И вообще, это может не работать!
        GlobalPathfinder = (BaseAI.PathFinder)FindObjectOfType(typeof(BaseAI.PathFinder));
        if (GlobalPathfinder == null)
        {
            Debug.Log("Не могу найти глобальный планировщик!");
            throw new System.ArgumentNullException("Can't find global pathfinder!");
        }

        //  Создаём целевую точку из объекта на сцене. В целом это должно задаваться в рамках алгоритма как-то
        FinishPoint = new BaseAI.PathNode(finish.transform.position, Vector3.zero);
        lastPathfinderRequest = -5.0f;
    }

    /// <summary>
    /// Движение ног - болтаем туда-сюда
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
    public void UpdatePathListDelegate(List<BaseAI.PathNode> pathNodes)
    {
        if (pathUpdateRequested == false)
        {
            //  Пока мы там путь строили, уже и не надо стало - выключили запрос
            return;
        }
        //  Просто перекидываем список, и всё
        plannedPath = pathNodes;
        pathUpdateRequested = false;
    }

    /// <summary>
    /// Запрос на достроение пути - должен сопровождаться довольно сложными проверками. Если есть целевая точка,
    /// и если ещё не дошли до целевой точки маршрута, и если количество оставшихся точек меньше чем MinimumPathNodesLeft - жуть.
    /// </summary>
    private bool RequestPathfinder()
    {
        if (FinishPoint == null || pathUpdateRequested || plannedPath != null) return false;
        if (Time.fixedTime - lastPathfinderRequest < 0.5f) return false;

        //  Тут ещё бы проверить, что финальная точка в нашем текущем списке точек не совпадает с целью, иначе плохо всё будет
        if (Vector3.Distance(transform.position, FinishPoint.Position) < movementProperties.epsilon ||
            currentPath != null && Vector3.Distance(currentPath[currentPath.Count-1].Position, FinishPoint.Position) < movementProperties.epsilon)
        {
            //  Всё, до цели дошли, сушите вёсла
            FinishPoint = null;
            plannedPath = null;
            currentPath = null;
            pathUpdateRequested = false;
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
        pathUpdateRequested = true;
        lastPathfinderRequest = Time.fixedTime;
        GlobalPathfinder.BuildRoute(startOfRoute, FinishPoint, movementProperties, UpdatePathListDelegate);
        
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
            if (distanceToTarget >= movementProperties.epsilon * 0.5f || currentTarget.TimeMoment - Time.fixedTime > movementProperties.epsilon * 0.1f) return true;
            //  Иначе удаляем её из маршрута и берём следующую
            currentPath.RemoveAt(0);
            if (currentPath.Count > 0)
            {
                //  Берём очередную точку и на выход (но точку не извлекаем!)
                currentTarget = currentPath[0];
                return true;
            }
            else
            {
                currentTarget = null;
                currentPath = null;
                //  А вот тут надо будет проверять, есть ли уже построенный маршрут
                RequestPathfinder();
                Debug.Log("Запрошено построение маршрута");
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
        else
            RequestPathfinder();

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
    bool TryToJump()
    {
        if (isJumpimg == true) return false;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            var rb = GetComponent<Rigidbody>();
            //  Сбрасываем скорость перед прыжком
            rb.velocity = Vector3.zero;
            var jump = transform.forward + 2 * transform.up;
            float jumpForce = movementProperties.jumpForce;
            rb.AddForce(jump * jumpForce, ForceMode.Impulse);
            isJumpimg = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Очередной шаг бота - движение
    /// </summary>
    /// <returns>false, если требуется обновление точки маршрута</returns>
    bool MoveBot()
    {
        //  Выполняем обновление текущей целевой точки
        if (!UpdateCurrentTargetPoint())
            //  Это ситуация когда идти некуда - цели нет
            return false;

        //  Если находимся в прыжке, то ничего делать не надо
        if (CheckJumping()) return false;
        //  Это зачем - непонятно
        if (TryToJump()) return true;

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

        //  Время прибытия - оставшееся время
        var remainedTime = currentTarget.TimeMoment - Time.fixedTime;
        if (remainedTime < movementProperties.epsilon)
        {
            transform.position = transform.position + actualStep * transform.forward;
        }
        else
        {
            //  Дедлайн ещё не скоро!!! Стоим спим
            if (currentTarget.Distance(transform.position) < movementProperties.epsilon)
                return true;

            transform.position = transform.position + actualStep * transform.forward / remainedTime;
        }
        return true;
    }

    /// <summary>
    /// Вызывается каждый кадр
    /// </summary>
    void Update()
    {
        //  Фрагмент кода, отвечающий за вставание
        var vertical_angle = Vector3.Angle(Vector3.up, transform.up);
        if (vertical_angle > max_angle)
        {
            GetComponent<Rigidbody>().AddForceAtPosition(5 * force * Vector3.up, transform.position + 3.0f * transform.up, ForceMode.Force);
        };

        if (!walking) return;

        //  Собственно движение
        if(MoveBot())
            MoveLegs();
    }
}
