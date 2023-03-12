using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class RVO_Crowd_Generator : MonoBehaviour
{
    public GameObject prefab;
    public GameObject AgentsParent;
    GameObject simulator;
    int agentID;

    public float maxX;
    public float maxY;

    public int agents_number;

    public bool Salon = false;
    public bool Pasillo = false;
    public bool Cruce = false;

    public bool AgentesEnMiDireccion = false;
    public int NumAgentesEnMiDireccion;
    float x_p;
    float z_p;

    //private List<Vector2, int> PositionsInitializedAgents = new List<Vector2, int>();

    private Dictionary<int, Vector2> PositionsInitializedAgents = new Dictionary<int, Vector2>();

    private List<Agent> Agentes = new List<Agent>();

    public Material SpecialMat;

    int aux = 0;

    private Vector2 agent_goal;

    private Vector2 goal_prueba;

    private float x_goal, z_goal;

    private Vector2 MyInitPosition;

    [Header("Debug")]
    public float neighborDist = 5.0f;
    public int MaxNeighbors = 10;
    public float TimeHorizon = 10.0f;
    public float TimeHorizonObst = 10.0f;
    public float radius = 0.5f;
    public float maxSpeed = 6;
    public Vector2 rvo_velocity = new Vector2(0,0);

    private void Awake()
    {
        Debug.Log("Comienzo el awake");
        RVO.Simulator.Instance.setTimeStep(0.25f);

        RVO.Vector2 velocity = new RVO.Vector2(rvo_velocity.x, rvo_velocity.y);
        RVO.Simulator.Instance.setAgentDefaults(neighborDist, MaxNeighbors, TimeHorizon, TimeHorizonObst, radius+0.1f, maxSpeed, velocity);
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Comienzo el start");

        for (int i = 0; i < agents_number; ++i)
        {
            CreateAgent();
        }
        Debug.Log("Voy a crear obstáculos");
        CreateObstacles();
        if (Pasillo)
        {
            Debug.Log("Voy a crear agente espcial");
            CreateSpecialAgent();
            if (AgentesEnMiDireccion) for (int i = 0; i < NumAgentesEnMiDireccion; ++i) CreateAgentInMiDirection();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        RVO.Simulator.Instance.setTimeStep(Time.deltaTime);
        foreach (Agent a in Agentes) a.SetPreferredVelocity();
        RVO.Simulator.Instance.doStep();
        foreach (Agent a in Agentes)
        {
            a.UpdatePos();
        }
       // CheckPrefabsToEliminate();
    }

    

    private void Update()
    {
     
    }



    public void CreateAgent()
    {
        CreateRandomPosition();
        Vector3 v_p = new Vector3(x_p, 0.0f, z_p);

        float y_r = UnityEngine.Random.Range(0f, 360f);
        Quaternion v_r = Quaternion.Euler(0, y_r, 0);

        MyInitPosition = new Vector2(x_p, z_p);
        agent_goal = CreateAgentGoal(false); //true = Como el agente especial, false = al contrario

        Debug.Log("La posicion del agente normal es " + v_p);
        var a = Instantiate(prefab, v_p, v_r, AgentsParent.transform);

        Agent newAgent = a.transform.GetChild(2).GetComponent<Agent>();
        agentID = RVO.Simulator.Instance.addAgent(new RVO.Vector2(v_p.x, v_p.z));

        SpecifyAgentParameters(v_p, newAgent, agentID, agent_goal, maxSpeed, new Vector2(maxX, maxY), false, false);
    }

    public void SpecifyAgentParameters(Vector3 pos, Agent agent, int id, Vector2 goal, float speed, Vector2 diam, bool IamSpecial, bool SameDirectionAsSpecial)
    {
        SetInitialCoordinates(pos.x, pos.z, agent);
        agent.SetID(id);
        agent.setMaxSpeed(speed);
        agent.SetMaximumDiameter(diam.x, diam.y);
        agent.SetNewGoal(goal.x, goal.y);
        SetEscenarioAgent(agent);
        AddAgent(agent);
        if(IamSpecial) agent.especial(SpecialMat);
        else if (SameDirectionAsSpecial) agent.DirectionAsSpecial();

        PositionsInitializedAgents.Add(id, new Vector2(pos.x, pos.z));


    }

    public void CreateSpecialAgent()
    {
        Debug.Log("PASO 1: Voy a crear agente especial");

        float x = UnityEngine.Random.Range(-(maxX - radius), -(maxX - 5f));
        float z = UnityEngine.Random.Range(-(maxY - radius), (maxY - radius));
        Vector3 v_p = new Vector3(x, 0.0f, z);

        MyInitPosition = new Vector2(x, z);

        float y_r = UnityEngine.Random.Range(0f, 360f);
        Quaternion v_r = Quaternion.Euler(0, y_r, 0);

        //Creo el goal aqui, para hacerlo accesible para RVOCharacterController.

        agent_goal = CreateAgentGoal(true); //true = Como el agente especial, false = al contrario

        // Debug.Log("Goal antes de instantiate = " + agent_goal);

        //INSTANCIAR EL PREFAB COMPLETO
        var a = Instantiate(prefab, v_p, v_r, AgentsParent.transform);

        //AGENTE DEL PREFAB
        Agent newAgent = a.transform.GetChild(2).GetComponent<Agent>();

        agentID = RVO.Simulator.Instance.addAgent(new RVO.Vector2(v_p.x, v_p.z));

        SpecifyAgentParameters(v_p, newAgent, agentID, agent_goal, maxSpeed, new Vector2(maxX, maxY), true, false);

    }

    public void CreateAgentInMiDirection()
    {
        //creo v_p, y_r, agent_goal, instancio, pillo newAgent, inicializo datos, creo RVO AGENT

        CreatePositionAgentInMiDirection();
        float y_r = UnityEngine.Random.Range(0f, 360f);
        Quaternion v_r = Quaternion.Euler(0, y_r, 0);

        Vector3 v_p = new Vector3(x_p, 0.0f, z_p);
        MyInitPosition = new Vector2(x_p, z_p);

        agent_goal = CreateAgentGoal(true);

        Debug.Log("el agent goal que creo es " + agent_goal);

        var a = Instantiate(prefab, v_p, v_r, AgentsParent.transform);

        //AGENTE DEL PREFAB
        Agent newAgent = a.transform.GetChild(2).GetComponent<Agent>();

        agentID = RVO.Simulator.Instance.addAgent(new RVO.Vector2(v_p.x, v_p.z));

        SpecifyAgentParameters(v_p, newAgent, agentID, agent_goal, maxSpeed, new Vector2(maxX, maxY), false, true);



    }

    private void CheckInitializedAgents(float x, float z)
    {
        bool exists = false;
        foreach (Vector2 v in PositionsInitializedAgents.Values)
        {
            if (v.x == x && v.y == z) exists = true;
        }

        if (exists) CreateRandomPosition(); 
        else
        {
            x_p = x;
            z_p = z;
        }
    }

    private void CreateRandomPosition()
    {
        if (Salon) SalonF();
        else if (Pasillo) PasilloF();
        else if (Cruce)
        {
            CruceF(); ++aux;
        }
    }

    void AddAgent(Agent a)
    {
        Agentes.Add(a);
    }

    public void RemoveAgent(Agent a)
    {
        Debug.Log("Voy a eliminar al agente con id = " + a.GetID());
        Agentes.Remove(a);


        Debug.Log("Tamaño del dict = " + PositionsInitializedAgents.Count);

        foreach (Vector2 v in PositionsInitializedAgents.Values)
        {
            Debug.Log(v);
        }

        PositionsInitializedAgents.Remove(a.GetID());

        Debug.Log("lo elimino");
        Debug.Log("Tamaño del dict = " + PositionsInitializedAgents.Count);

        foreach (Vector2 v in PositionsInitializedAgents.Values)
        {
            Debug.Log(v);
        }
    }

    private void CreateObstacles()
    {
        GameObject[] _obstaculos = GameObject.FindGameObjectsWithTag("Pared");
        GameObject[] _obstaculos2 = GameObject.FindGameObjectsWithTag("Obstaculos");
        foreach (GameObject o in _obstaculos)
        {
            Vector3[] vertices = o.gameObject.GetComponent<MeshFilter>().mesh.vertices;
            List<RVO.Vector2> obstaculo = new List<RVO.Vector2>();
            foreach (Vector3 v in vertices)
            {
                obstaculo.Add(new RVO.Vector2(v.x, v.z));
            }
            RVO.Simulator.Instance.addObstacle(obstaculo);
        }
        foreach (GameObject o in _obstaculos2)
        {
            Vector3[] vertices = o.gameObject.GetComponent<MeshFilter>().mesh.vertices;
            List<RVO.Vector2> obstaculo = new List<RVO.Vector2>();
            foreach (Vector3 v in vertices)
            {
                obstaculo.Add(new RVO.Vector2(v.x, v.z));
            }
            RVO.Simulator.Instance.addObstacle(obstaculo);
        }
    }

    public int getID()
    {
        return agentID;
    }

    private void SalonF()
    {
        float x = UnityEngine.Random.Range(-maxX, maxY);
        float z = UnityEngine.Random.Range(-maxX, maxY);
        CheckInitializedAgents(x, z);
    }

    private void PasilloF()
    {
        float x = UnityEngine.Random.Range((maxX - 5f), (maxX - radius));
        float z = UnityEngine.Random.Range(-(maxY-radius), (maxY-radius));
        CheckInitializedAgents(x, z);
    }

    private void CruceF()
    {
        float x = 0, z = 0;

        if (aux % 4 == 0)
        {
            Debug.Log("entro aqui = " + aux % 4);

            //Pasillo 1
            x = UnityEngine.Random.Range((maxX - 5f), (maxX - radius)); // 15, 19.5
            z = UnityEngine.Random.Range(-(maxY - radius), (maxY - radius)); // -4.5, 4.
        }

        else if (aux % 4 == 1)
        {
          //  Debug.Log("entro aqui = " + aux%4);

            //Pasillo 2
            x = UnityEngine.Random.Range(-(maxX - radius),- (maxX - 5f)); //-19.5, -15
            z = UnityEngine.Random.Range(-(maxY - radius), (maxY - radius)); // -4.5, 4.5
        }
        else if (aux % 4 == 2)
        {
            //Pasillo 3
       //     Debug.Log("entro aqui = " + aux % 4);

            z = UnityEngine.Random.Range((maxX - 5f), (maxX - radius)); // 15, 19.5
            x = UnityEngine.Random.Range(-(maxY - radius), (maxY - radius)); // -4.5, 4.5
        }
        else if (aux % 4 == 3)
        {
            //Pasillo 4
          //  Debug.Log("entro aqui = " + aux % 4);

            z = UnityEngine.Random.Range(-(maxX - radius), -(maxX - 5f));//-19.5, -15
            x = UnityEngine.Random.Range(-(maxY - radius), (maxY - radius));// -4.5, 4.5
        }        
        CheckInitializedAgents(x, z);
    }

    private void CreatePositionAgentInMiDirection()
    {
        float x = UnityEngine.Random.Range(-(maxX - radius), -(maxX - 5f));
        float z = UnityEngine.Random.Range(-(maxY - radius), (maxY - radius));
        CheckInitializedAgentsInMyDirection(x, z);
    }

    private void CheckInitializedAgentsInMyDirection(float x, float z)
    {
        bool exists = false;
        foreach (Vector2 v in PositionsInitializedAgents.Values)
        {
            if (v.x == x && v.y == z) exists = true;
        }

        if (exists) CreatePositionAgentInMiDirection();
        else
        {
          //  PositionsInitializedAgents.Add(agentID, new Vector2(x, z));
            x_p = x;
            z_p = z;
        }
    }

    private void SetEscenarioAgent(Agent newAgent)
    {
        if (Salon) newAgent.SetEscenario("Salon");
        else if (Pasillo) newAgent.SetEscenario("Pasillo");
        else if (Cruce) newAgent.SetEscenario("Cruce");
    }

    private void SetInitialCoordinates(float x, float z, Agent a)
    {
        a.SetInitialCoordinates(x,z);
    }

    public int GetAux()
    {
        return aux;
    }

    public Vector2 RVOCrowdGenerator_GetGoal()
    {
        return agent_goal;
    }

    private void CheckPrefabsToEliminate()
    {
        Debug.Log("Recorro al padre");

        List<int> ChildsToDestroy = new List<int>();
        for (int i = 0; i < AgentsParent.transform.childCount; ++i)
        {
            GameObject element = AgentsParent.transform.GetChild(i).gameObject;
            if (!element.activeSelf) ChildsToDestroy.Add(i);
        }

        for(int i = 0; i < ChildsToDestroy.Count; ++i)
        {
            GameObject element = AgentsParent.transform.GetChild(i).gameObject;
            for (int j = 0; j < element.transform.childCount; ++j)
            {
                Debug.Log("intento eliminar el " + j);
                Destroy(element.transform.GetChild(j).gameObject);
            }
            Debug.Log("intento eliminar el propio objeto");
            Destroy(element.gameObject);
        }

        Debug.Log("en principio ya he terminado");

    }

    private Vector2 CreateAgentGoal(bool dir_sp) { //true = dirección del agente especial, flase = la contraria 

        if (Salon)
        {
            x_goal = UnityEngine.Random.Range(-(maxX - 1), (maxX - 1));
            z_goal = UnityEngine.Random.Range(-(maxY - 1), (maxY - 1));
        }

        else if (Pasillo)
        {
            if (dir_sp)
            {
                x_goal = (maxX);
                z_goal = UnityEngine.Random.Range(-(maxY - 0.5f), (maxY - 0.5f));
            }
            else
            {
                x_goal = -(maxX);
                z_goal = UnityEngine.Random.Range(-(maxY - 0.5f), (maxY - 0.5f));
            }
        }
        
        else if (Cruce)
        {
            float x = MyInitPosition.x;
            float z = MyInitPosition.y;

            if (x > -(maxX - radius) && x < -(maxX - 5f)) // -19.5, -15
            {
                //Pasillo 1
                x_goal = maxX; // 15, 19.5
                z_goal = UnityEngine.Random.Range(-(maxY - radius), (maxY - radius)); // -4.5, 4.5
            }

            else if (x > (maxX - 5f) && x < (maxX - 0.5f)) // 15, 19.5
            {
                //Pasillo 2
                x_goal = -maxX; //-19.5, -15
                z_goal = UnityEngine.Random.Range(-(maxY - radius), (maxY - radius)); // -4.5, 4.5
            }
            else if (z > -(maxX - radius) && z < -(maxX - 5f)) //-19.5, -15
            {
                //Pasillo 3
                z_goal = maxX; // 15, 19.5
                x_goal = UnityEngine.Random.Range(-(maxY - radius), (maxY - radius)); // -4.5, 4.5
            }
            else if (z > (maxX - 5f) && x < (maxX - 0.5f)) // 15, 19.5
            {
                //Pasillo 4
                z_goal = -maxX;//-19.5, -15
                x_goal = UnityEngine.Random.Range(-(maxY - radius), (maxY - radius));// -4.5, 4.5
            }
        }
        //Creo el objeto GOAL
        return new Vector2(x_goal, z_goal);
    }
    
}
