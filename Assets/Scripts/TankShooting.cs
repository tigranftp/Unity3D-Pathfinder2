using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BaseAI
{

    public class TankShooting : MonoBehaviour
    {
        public GameObject Bullet;
        public GameObject platf;
        public float shootTime = 0.0f;

        void Shoot()
        {
            //  Стрельба !!!!
            //  Таким образом мы можем создать экземпляр объекта (шаблона объекта)
            var bullet = Instantiate(Bullet) as GameObject;
            //   Указываем для вновь созданного объекта положение в пространстве
            bullet.transform.position = transform.position + transform.forward * 2f + transform.up * 1.6f;

            bullet.GetComponent<Rigidbody>().velocity = 12.0f * transform.forward + Vector3.up * 12.0f;
            bullet.GetComponent<SuicideBullet>().SetStartTankPosition(transform.position);
            //bullet.GetComponent<Rigidbody>().angularVelocity = 50.0f * transform.right;
            //  Так можно создавать примитивы объектов (но нам это не очень интересно)
            //var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }

        // Update is called once per frame
        void Update()
        {
            var targetPoint = transform.position + transform.forward * 33.62f;
            PathNode target = new PathNode(targetPoint, Vector3.zero);
            target.TimeMoment = Time.time + 2.6f;

            if (Time.time > shootTime + 0.2f && platf.GetComponent<IBaseRegion>().Contains(target))
            {
                shootTime = Time.time;
                Shoot();
            }

            if (Input.GetButtonDown("Fire1"))
                Shoot();
        }
    }
}