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
    public Vector3 lastPosition;
    public Vector3 lastCollisionCheckPosition;

    public float damage;
    public float speed;
    public float destroyTime;

    public int bulletChunk;

    public bool destroy;
    public int teamID;
    public int hitID;
    public float ignoreCollision;
    
    public Bullet(int team, Vector3 positon, quaternion rotation, Vector3 scale, float _speed, float _damage, float currentTime, float _destroyTime, int _bulletChunk)
    {
        teamID = team;
        transformMatrix = Matrix4x4.TRS(positon, rotation, scale);
        lastCollisionCheckPosition = positon;
        lastPosition = positon;
        speed = _speed;
        damage = _damage;
        destroyTime = _destroyTime;
        destroy = false;
        ignoreCollision = currentTime + 0.1f;
        bulletChunk = _bulletChunk;
        hitID = -1;
    }
}

public struct MoveBullets : IJobParallelFor
{
    [ReadOnly] float time;
    [ReadOnly] float deltaTime;
    [ReadOnly] int currentBulletChunk;
    [ReadOnly] NativeArray<Agent> agents;
    NativeArray<Bullet> bullets;

    // bullets hits own ship
    public MoveBullets(NativeArray<Bullet> _bullets, NativeArray<Agent> _agents, float _time, float _deltaTime, int _currentBulletChunk)
    {
        agents = _agents;
        bullets = _bullets;
        time = _time;
        deltaTime = _deltaTime;
        currentBulletChunk = _currentBulletChunk;
    }

    public void Execute(int index)
    {
        bool destroy = false;
        int hitID;

        if(CheckCollision(index, out hitID))
        {
            destroy = true;
        }
        Bullet bullet = bullets[index];

        if (bullet.destroyTime < time || destroy)
        {
            bullet.hitID = hitID;
            bullet.destroy = true;
        }
        else
        {
            Vector3 forward = new Vector3(bullet.transformMatrix[0, 2], bullet.transformMatrix[1, 2], bullet.transformMatrix[2, 2]);
            bullet.lastPosition = bullet.transformMatrix.GetPosition();
            if (bullet.bulletChunk == currentBulletChunk) bullet.lastCollisionCheckPosition = bullet.lastPosition;
            bullet.transformMatrix = Matrix4x4.TRS(bullet.lastPosition + forward * deltaTime * bullet.speed, bullet.transformMatrix.rotation, bullet.transformMatrix.lossyScale);
        }
        bullets[index] = bullet;
    }

    public bool CheckCollision(int index, out int hitID)
    {
        Bullet bullet = bullets[index];
        hitID = -1;

        if (bullet.ignoreCollision > time || bullet.bulletChunk != currentBulletChunk) return false;
        Vector3 position = bullet.transformMatrix.GetPosition();

        int closest = -1;
        float closestDistance = float.PositiveInfinity;
        Vector3 middle = bullet.lastCollisionCheckPosition + (position - bullet.lastCollisionCheckPosition) / 2;



        for (int i = 0; i < agents.Length; i++)
        {
            Agent agent = agents[i];
            if (agent.agentTeam == bullet.teamID || agent.position.x - agent.colliderSize > math.max(middle.x, position.x))
            {
                break;
            }

            float currentDistance = Vector3.SqrMagnitude(agent.position - position);
            if (currentDistance < closestDistance)
            {
                closestDistance = currentDistance;
                closest = i;
            }
        }

        if (closest == -1) return false;

        Agent closestAgent = agents[closest];

        Vector3 directionStart = (closestAgent.position - bullet.lastCollisionCheckPosition), directionEnd = (closestAgent.position - position);


        if (directionStart.magnitude < closestAgent.colliderSize || directionEnd.magnitude < closestAgent.colliderSize)
        {
            hitID = closest;
            return true;
        }
        else
        {
            Vector3 lineVector = position - bullet.lastCollisionCheckPosition;
            float lineVectorLenght = lineVector.magnitude;

            float t = ((closestAgent.position.x - position.x) * (bullet.lastCollisionCheckPosition.x - position.x) +
                (closestAgent.position.y - position.y) * (bullet.lastCollisionCheckPosition.y - position.y) +
                (closestAgent.position.z - position.z) * (bullet.lastCollisionCheckPosition.z - position.z)) / (lineVectorLenght * lineVectorLenght);

            if (t < 0 || t > 1) return false;
            else
            {
                hitID = closest;
                return true;
            }

        }
    }
}
