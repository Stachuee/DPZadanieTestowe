using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Jobs;

public class AgentManager : MonoBehaviour
{
    public static AgentManager Instance { get; private set; }

    [SerializeField] int agentCount;
    
    [SerializeField]
    List<Agent> allAgents = new List<Agent>();
    List<Transform> allAgentTransforms = new List<Transform>();
    [SerializeField]
    List<Squadron> squadron = new List<Squadron>();
    [SerializeField]
    List<Bullet> bullets = new List<Bullet>();
    [SerializeField] 
    List<CaptureZone> captureZones = new List<CaptureZone>();


    Dictionary<int, string> agentNames = new Dictionary<int, string>(); // remember to remove names after destroying agent

    [SerializeField] Vector3 playfieldCenter;
    [SerializeField] Vector3 playfieldSize;


    int nextAgentId = 0;
    [SerializeField, Min(1)] int bulletsChunks;
    int currentBulletChunkCheck;
    int currentBulletChunk;

    NativeArray<Agent> agentsNativeArray;
    NativeArray<Agent> readOnlyAgentsNativeArray;
    NativeArray<Squadron> squadronNativeArray;
    NativeArray<Bullet> bulletsNativeArray;
    NativeArray<CaptureZone> captureZonesNativeArray;
    TransformAccessArray transformAccessArray;

    JobHandle agentSteeringHandle;
    JobHandle bulletHandle;
    JobHandle zonesHandle;



    [SerializeField] Mesh bulletMesh;
    [SerializeField] Material bulletMaterial;
    RenderParams bulletRenderParams;

    [SerializeField] AgentTypeSO type;
    [SerializeField] AgentWeaponsBlasterSO blaster;

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

        WarpSquadron(1, type, blaster, 0, 0);
        WarpSquadron(1, type, blaster, 1, 1);
        WarpSquadron(1, type, blaster, 2, 0);
        WarpSquadron(1, type, blaster, 3, 1);
    }


    private void Update()
    {

        SortAgents();

        agentsNativeArray = new NativeArray<Agent>(allAgents.ToArray(), Allocator.TempJob);
        readOnlyAgentsNativeArray = new NativeArray<Agent>(allAgents.ToArray(), Allocator.TempJob);
        squadronNativeArray = new NativeArray<Squadron>(squadron.ToArray(), Allocator.TempJob);
        bulletsNativeArray = new NativeArray<Bullet>(bullets.ToArray(), Allocator.TempJob);
        captureZonesNativeArray = new NativeArray<CaptureZone>(captureZones.ToArray(), Allocator.TempJob);
        transformAccessArray = new TransformAccessArray(allAgentTransforms.ToArray());

        AgentCollision agentCollisionJob = new AgentCollision(
            agentsNativeArray);
        agentCollisionJob.Run();

        SquadronUtil squadronUtil = new SquadronUtil(
            squadronNativeArray,
            readOnlyAgentsNativeArray
        );

        squadronUtil.Run();


        for (int i = 0; i < allAgents.Count; i++)
        {
            transformAccessArray[i].position = agentsNativeArray[i].position; // not sure about that. Could be using this wrong
        }

        AgentSteering agentSteeringJob = new AgentSteering(
            agentsNativeArray, 
            readOnlyAgentsNativeArray,
            squadronNativeArray,
            Time.deltaTime,
            Time.time,
            playfieldSize,
            playfieldCenter
            );

        agentSteeringHandle = agentSteeringJob.Schedule(transformAccessArray);


        CheckCaptureZone checkCaptureZone = new CheckCaptureZone(
            readOnlyAgentsNativeArray,
            Time.deltaTime,
            captureZonesNativeArray
            );

        zonesHandle = checkCaptureZone.Schedule(captureZonesNativeArray.Length, 4);


        currentBulletChunkCheck = (currentBulletChunkCheck + 1) % bulletsChunks;

        MoveBullets moveBullets = new MoveBullets(
            bulletsNativeArray,
            readOnlyAgentsNativeArray,
            Time.time,
            Time.deltaTime,
            currentBulletChunk
            );

        bulletHandle = moveBullets.Schedule(bulletsNativeArray.Length, 32);
        
    }


    private void LateUpdate()
    {
        agentSteeringHandle.Complete();
        bulletHandle.Complete();
        zonesHandle.Complete();

        for (int i = 0; i < allAgents.Count; i++)
        {
            allAgents[i] = agentsNativeArray[i];
            if (allAgents[i].blaster.fired)
            {
                Agent shooter = allAgents[i];
                shooter.blaster.fired = false;

                currentBulletChunk = (currentBulletChunk + 1) % bulletsChunks;

                bullets.Add(new Bullet(
                    shooter.agentTeam,
                    shooter.position + shooter.blaster.barrelOrientation * shooter.colliderSize * 1.5f,
                    Quaternion.LookRotation(shooter.blaster.barrelOrientation, allAgents[i].up),
                    Vector3.one / 5,
                    shooter.blaster.missleSpeed,
                    shooter.blaster.damage,
                    Time.time,
                    Time.time + shooter.blaster.missleLifetime,
                    currentBulletChunk));

                allAgents[i] = shooter;
            }
        }

        for(int i = 0; i < bulletsNativeArray.Length; i++)
        {
            bullets[i] = bulletsNativeArray[i];
        }

        for(int i = 0; i < captureZonesNativeArray.Length; i++)
        {
            captureZones[i] = captureZonesNativeArray[i];
        }

        for(int i = squadronNativeArray.Length - 1; i >= 0; i--)
        {
            if(squadronNativeArray[i].squadronUnitCount > 0)
            {
                squadron[i] = squadronNativeArray[i];
            }
            else
            {
                IconOverlayUI.Instance.RemoveSquadronWaypoint(squadron[i].squdronID);
                squadron.RemoveAt(i);
            }
        }

        ClearBullets();
        ClearAgents();

        DrawBullets();


        agentsNativeArray.Dispose();
        transformAccessArray.Dispose();
        readOnlyAgentsNativeArray.Dispose();
        squadronNativeArray.Dispose();
        bulletsNativeArray.Dispose();
        captureZonesNativeArray.Dispose();
    }

    public void WarpSquadron(int unitCount, AgentTypeSO type, AgentWeaponsBlasterSO blaster, int squadronID, int teamID)
    {
        int matIndex = TeamManager.GetTeamWarpMaterialID(teamID);

        int squadronIndex = squadron.FindIndex(squadron => squadron.squdronID == squadronID);
        if (squadronIndex == -1)
        {
            Squadron order = new Squadron()
            {
                formation = Squadron.Formation.Defensive,
                rallyPoint = new Vector3(0, 0, 0),
                squdronID = squadronID
            };
            squadron.Add(order);
            IconOverlayUI.Instance.CreateSquadronWaypoint(squadronID, teamID);
        }
        
        for (int i = 0; i < unitCount; i++)
            WarpAgent(
                type,
                blaster,
                TeamManager.GetRandomTeamWarpPosition(teamID),
                matIndex,
                squadronID,
                teamID
                );

    }

    public void WarpAgent(AgentTypeSO type, AgentWeaponsBlasterSO blasterType, Vector3 spawnPoint, int warpMatIndex, int squadron, int team)
    {
        GameObject newAgent = AgentPooling.Instance.NewAgentFromPool();
        newAgent.GetComponent<AgentInnit>().Innit(team, warpMatIndex);
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
            forward = TeamManager.GetTeamWarpRotation(team),
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
            captureRate = type.GetCaptureRate(),
            blaster = blaster,
        };
        
        //string agentName = AgentNames.GetRandomName();
        toAdd.id = GetNextId();
        newAgent.transform.name = toAdd.id.ToString();
        agentNames.Add(toAdd.id, AgentNames.GetRandomName());

        allAgentTransforms.Add(newAgent.transform);
        allAgents.Add(toAdd);
    }

    void ClearBullets()
    {
        bullets.ForEach(x =>
        {
            if(x.destroy && x.hitID != -1)
            {
                Agent agent = allAgents[x.hitID];
                agent.agentHP -= x.damage;
                allAgents[x.hitID] = agent;
            }
        });
        bullets.RemoveAll(x => x.destroy);
    }

    void ClearAgents()
    {
        List<int> toRemove = new List<int>();

        for (int i = 0; i < allAgents.Count; i++)
        {
            if (allAgents[i].agentHP <= 0)
            {
                toRemove.Add(i);
            }
        }

        for(int i = toRemove.Count - 1; i >= 0; i--)
        {
            agentNames.Remove(allAgents[toRemove[i]].id);
            allAgents.RemoveAt(toRemove[i]);
            AgentPooling.Instance.ReturnAgentToPool(allAgentTransforms[toRemove[i]].gameObject);
            allAgentTransforms.RemoveAt(toRemove[i]);
        }
    }

    void DrawBullets()
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

    public List<Squadron> GetSquadrons()
    {
        return squadron;
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

        foreach (Squadron order in squadron)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(order.rallyPoint, 1);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(order.rallyPoint, Squadron.DEFENSIVE_FORMATION_ACCEPTED_RADIUS);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(order.rallyPoint, Squadron.OFFENSIVE_FORMATION_ACCEPTED_RADIUS);
        }

        captureZones.ForEach(zone =>
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(zone.position, zone.size);
        });
    }
}
