using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;
using Unity.Burst;



[System.Serializable]
public struct Bullet
{
    public Matrix4x4 transformMatrix;
    public Vector3 lastFramePosition;
    public float speed;
    public float destroyTime;

    public bool destroy;
    
    public Bullet(Vector3 positon, quaternion rotation, Vector3 scale, float _speed, float _destroyTime)
    {
        transformMatrix = Matrix4x4.TRS(positon, rotation, scale);
        lastFramePosition = positon;
        speed = _speed;
        destroyTime = _destroyTime;
        destroy = false;
    }
}

public struct MoveBullets : IJobParallelFor
{
    [ReadOnly] float time;
    [ReadOnly] float deltaTime;
    NativeArray<Bullet> bullets;

    public MoveBullets(NativeArray<Bullet> _bullets, float _time, float _deltaTime)
    {
        bullets = _bullets;
        time = _time;
        deltaTime = _deltaTime;
    }

    public void Execute(int index)
    {
        Bullet bullet = bullets[index];

        if (bullet.destroyTime < time)
        {
            bullet.destroy = true;
        }
        else
        {
            bullet.lastFramePosition = bullet.transformMatrix.GetPosition();
            Vector3 forward = new Vector3(bullet.transformMatrix[0, 2], bullet.transformMatrix[1, 2], bullet.transformMatrix[2, 2]);
            bullet.transformMatrix = Matrix4x4.TRS(bullet.lastFramePosition + forward * deltaTime * bullet.speed, bullet.transformMatrix.rotation, bullet.transformMatrix.lossyScale);
        }
        bullets[index] = bullet;
    }

}
