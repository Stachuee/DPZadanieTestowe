using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Jobs;

public class AgentManager : MonoBehaviour
{
    public static AgentManager Instance;

    [SerializeField] int agentCount;
    
    [SerializeField]
    List<Agent> allAgents = new List<Agent>();
    List<Transform> allAgentTransforms = new List<Transform>();
    [SerializeField]
    List<SquadronOrders> orders = new List<SquadronOrders>();
    [SerializeField]
    List<Bullet> bullets = new List<Bullet>();


    Dictionary<int, string> agentNames = new Dictionary<int, string>(); // remember to remove names after destroying agent

    [SerializeField] Vector3 playfieldCenter;
    [SerializeField] Vector3 playfieldSize;


    int nextAgentId = 0;

    NativeArray<Agent> agentsNativeArray;
    NativeArray<Agent> readOnlyAgentsNativeArray;
    NativeArray<SquadronOrders> ordersNativeArray;
    NativeArray<Bullet> bulletsNativeArray;
    TransformAccessArray transformAccessArray;

    JobHandle agentSteeringHandle;
    JobHandle bulletHandle;


    [SerializeField] AgentTypeSO toSpawn;
    [SerializeField] AgentWeaponsBlasterSO blaster;



    [SerializeField] Mesh bulletMesh;
    [SerializeField] Material bulletMaterial;
    RenderParams bulletRenderParams;


    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Two agent managers on one scene");
        }
    }

    private void Start()
    {
        AgentPooling.Instance.CreatePool(agentCount);

        bulletRenderParams = new RenderParams(bulletMaterial);
        bulletRenderParams.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        bulletRenderParams.receiveShadows = false;


        for (int i = 0; i < 2; i++)
        {
            SpawnDivision(1, i, i%2);
        }
    }


    private void Update()
    {

        SortAgents();

        agentsNativeArray = new NativeArray<Agent>(allAgents.ToArray(), Allocator.TempJob);
        readOnlyAgentsNativeArray = new NativeArray<Agent>(allAgents.ToArray(), Allocator.TempJob);
        ordersNativeArray = new NativeArray<SquadronOrders>(orders.ToArray(), Allocator.TempJob);
        bulletsNativeArray = new NativeArray<Bullet>(bullets.ToArray(), Allocator.TempJob);
        transformAccessArray = new TransformAccessArray(allAgentTransforms.ToArray());

        AgentCollision agentCollisionJob = new AgentCollision(
            agentsNativeArray);
        agentCollisionJob.Run();


        for(int i = 0; i < allAgents.Count; i++)
        {
            transformAccessArray[i].position = agentsNativeArray[i].position; // not sure about that. Could be using this wrong
        }

        AgentSteering agentSteeringJob = new AgentSteering(
            agentsNativeArray, 
            readOnlyAgentsNativeArray,
            ordersNativeArray,
            Time.deltaTime,
            Time.time,
            playfieldSize,
            playfieldCenter
            );

        agentSteeringHandle = agentSteeringJob.Schedule(transformAccessArray);


        MoveBullets moveBullets = new MoveBullets(
            bulletsNativeArray,
            Time.time,
            Time.deltaTime);

        bulletHandle = moveBullets.Schedule(bulletsNativeArray.Length, 32);
        
    }


    private void LateUpdate()
    {
        agentSteeringHandle.Complete();
        bulletHandle.Complete();

        for (int i = 0; i < allAgents.Count; i++)
        {
            allAgents[i] = agentsNativeArray[i];
            if (allAgents[i].blaster.fired)
            {
                Agent shooter = allAgents[i];
                shooter.blaster.fired = false;
                bullets.Add(new Bullet()
                {
                    destroyTime = Time.time + shooter.blaster.missleLifetime,
                    speed = shooter.blaster.missleSpeed,
                    transformMatrix = Matrix4x4.TRS(shooter.position, Quaternion.LookRotation(shooter.blaster.barrelOrientation, allAgents[i].up), Vector3.one/5),
                });


                allAgents[i] = shooter;
            }
        }

        for(int i = 0; i < bulletsNativeArray.Length; i++)
        {
            bullets[i] = bulletsNativeArray[i];
        }

        ClearBullets();
        DrawBullets();


        agentsNativeArray.Dispose();
        transformAccessArray.Dispose();
        readOnlyAgentsNativeArray.Dispose();
        ordersNativeArray.Dispose();
        bulletsNativeArray.Dispose();
    }

    public void SpawnDivision(int unitCount, int divisionID, int teamID)
    {
        for (int i = 0; i < unitCount; i++)
            SpawnAgent(
                toSpawn,
                blaster,
                new Vector3(playfieldCenter.x + Random.Range(-playfieldSize.x, playfieldSize.x) / 2, playfieldCenter.y + Random.Range(-playfieldSize.y, playfieldSize.y) / 2, playfieldCenter.z + Random.Range(-playfieldSize.z, playfieldSize.z) / 2),
                divisionID,
                teamID
                );
        SquadronOrders order = new SquadronOrders()
        {
            formation = SquadronOrders.Formation.Defensive,
            rallyPoint = new Vector3(playfieldCenter.x + Random.Range(-playfieldSize.x, playfieldSize.x) / 2, playfieldCenter.y + Random.Range(-playfieldSize.y, playfieldSize.y) / 2, playfieldCenter.z + Random.Range(-playfieldSize.z, playfieldSize.z) / 2),
            squdronID = divisionID
        };
        orders.Add(order);
    }

    public void SpawnAgent(AgentTypeSO type, AgentWeaponsBlasterSO blasterType, Vector3 spawnPoint, int squadron, int team)
    {
        GameObject newAgent = AgentPooling.Instance.NewAgentFromPool();
        newAgent.GetComponent<AgentInnit>().Innit(team);
        newAgent.transform.position = spawnPoint;

        Blaster blaster = new Blaster()
        {
            enabled = true,
            damage = blasterType.GetDamage(),
            fireRate = blasterType.GetFirerate(),
            missleSpeed = blasterType.GetMissleSpeed(),
            missleLifetime = blasterType.GetMissleLifeTime(),
        };

        Agent toAdd = new Agent() {

            position = spawnPoint,
            squadron = squadron,
            agentTeam = team,
            up = new Vector3(0, 1, 0),
            forward = new Vector3(1, 0, 0),
            colliderSize = type.GetColliderSize(),
            mass = type.GetMass(),
            dragCoefficient = type.GetDragCoefficient(),
            engineMaxStrenght = type.GetEngineStrenght(),
            breakingStrenght = type.GetBreakingStrenght(),
            maneuverabilityHorizontal = type.GetManeuverabilityHorizontal(),
            maneuverabilityVertical = type.GetManeuverabilityVertical(),
            rollSpeed = type.GetRollSpeed(),
            agentMaxHP = type.GetHp(),
            agentHP = type.GetHp(),
            avoidenenceRange = type.GetAvoidenenceRange(),
            scannerRange = type.GetScannerRange(),
            blaster = blaster,
        };
        
        //string agentName = AgentNames.GetRandomName();
        toAdd.id = GetNextId();
        newAgent.transform.name = toAdd.id.ToString();
        agentNames.Add(toAdd.id, AgentNames.GetRandomName());

        allAgentTransforms.Add(newAgent.transform);
        allAgents.Add(toAdd);
    }

    public void ClearBullets()
    {
        bullets.RemoveAll(x => x.destroy);
    }

    public void DrawBullets()
    {
        bullets.ForEach(bullet =>
        {
            Graphics.RenderMesh(bulletRenderParams, bulletMesh, 0, bullet.transformMatrix);
        });
    }

    void SortAgents()
    {
        for(int i = 1; i < allAgents.Count; i++)
        {
            for(int j = i - 1; j >= 0; j--)
            {
                if(allAgentTransforms[j].position.x - allAgents[j].colliderSize > allAgentTransforms[j + 1].position.x - allAgents[j + 1].colliderSize)
                {
                    Agent temp = allAgents[j];
                    allAgents[j] = allAgents[j + 1];
                    allAgents[j + 1] = temp;

                    Transform tempTransform = allAgentTransforms[j];
                    allAgentTransforms[j] = allAgentTransforms[j + 1];
                    allAgentTransforms[j + 1] = tempTransform;
                }
                else
                {
                    break;
                }
            }
        }
    }

    int GetNextId()
    {
        return nextAgentId++;
    }

    public Agent GetAgentById(int id)
    {
        return allAgents.Find(x => x.id == id);
    }

    public string GetAgentNameByID(int id)
    {
        return agentNames[id];
    }

    public int GetAgentCount()
    {
        return agentCount;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (Agent agent in allAgents)
        {
            Gizmos.DrawWireSphere(agent.position, agent.colliderSize);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(agent.position, agent.position + agent.steeringVector * 3);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(agent.position, agent.position + agent.blaster.barrelOrientation * 3);
        }

        Gizmos.color = Color.white;

        Gizmos.DrawWireCube(playfieldCenter, new Vector3(playfieldSize.x, playfieldSize.y, playfieldSize.z));

        foreach (SquadronOrders order in orders)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(order.rallyPoint, 1);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(order.rallyPoint, SquadronOrders.DEFENSIVE_FORMATION_ACCEPTED_RADIUS);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(order.rallyPoint, SquadronOrders.OFFENSIVE_FORMATION_ACCEPTED_RADIUS);
        }
    }
}
