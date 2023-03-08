using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class RVO_Crowd_Generator : MonoBehaviour
{
    public GameObject prefab;
    public GameObject parent;
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
    public Vector2 agent_goal;
    float x_p;
    float z_p;

    private List<Vector2> PositionsInitializedAgents = new List<Vector2>();

    private List<Agent> Agentes = new List<Agent>();

    public Material SpecialMat;

    int aux = 0;

    [Header("Debug")]
    public float neighborDist = 5.0f;
    public int MaxNeighbors = 10;
    public float TimeHorizon = 10.0f;
    public float TimeHorizonObst = 10.0f;
    public float radius = 0.5f;
    public float maxSpeed = 6;
    public RVO.Vector2 rvo_velocity = new RVO.Vector2(0.0f,0.0f);

    private void Awake()
    {
        Debug.Log("Comienzo el awake");
        RVO.Simulator.Instance.setTimeStep(0.25f);
        RVO.Simulator.Instance.setAgentDefaults(neighborDist, MaxNeighbors, TimeHorizon, TimeHorizonObst, radius+0.1f, maxSpeed, rvo_velocity);
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
    }

    

    private void Update()
    {
     
    }



    public void CreateAgent()
    {
        CreateRandomPosition();
        agentID = RVO.Simulator.Instance.addAgent(new RVO.Vector2(x_p, z_p));

        float y_r = UnityEngine.Random.Range(0f, 360f);

        Quaternion v_r = Quaternion.Euler(0, y_r, 0);
        Vector3 v_p = new Vector3(x_p, 0.0f, z_p);

        Agent newAgent = Instantiate(prefab, v_p, v_r, parent.transform).transform.GetChild(2).GetComponent<Agent>();
        Debug.Log("creo agente");
        newAgent.SetID(agentID);
        newAgent.SetMaximumDiameter(maxX, maxY);
        SetInitialCoordinates(x_p, z_p, newAgent);
        SetEscenarioAgent(newAgent);

        AddAgent(newAgent);
    }

    public void CreateSpecialAgent()
    {
        float x = UnityEngine.Random.Range(-(maxX - radius), -(maxX - 5f));
        float z = UnityEngine.Random.Range(-(maxY - radius), (maxY - radius));
        Vector3 v_p = new Vector3(x, 0.0f, z);

        float y_r = UnityEngine.Random.Range(0f, 360f);
        Quaternion v_r = Quaternion.Euler(0, y_r, 0);

        //Creo el goal aqui, para hacerlo accesible para RVOCharacterController.
        agent_goal = CreateAgentGoal();

        Debug.Log("Goal antes de instantiate = " + agent_goal);

        //INSTANCIAR EL PREFAB COMPLETO
        var a = Instantiate(prefab, new Vector3(0, 0, 0), v_r, parent.transform);

        //AGENTE DEL PREFAB
        Agent newAgent = a.transform.GetChild(2).GetComponent<Agent>();

        //CHARACTER CONTROLLER DEL PREFAB
        MotionMatching.RVO_Character_Controller rvo_Char_Controller = a.transform.GetChild(0).GetComponent<MotionMatching.RVO_Character_Controller>();

        //Accedo a las coordenadas iniciales del agente
        float newX = rvo_Char_Controller.x_ini;
        float newZ = rvo_Char_Controller.z_ini;
        Debug.Log("X_INI y Z_INI = " + newX + newZ);
        //Y se las inicializo
        SetInitialCoordinates(newX, newZ, newAgent);

        //Creo el nuevo agente RVO con esa posición
        agentID = RVO.Simulator.Instance.addAgent(new RVO.Vector2(newX, newZ));

        //Más datos para el agente
        newAgent.SetID(agentID);
        newAgent.SetMaximumDiameter(maxX, maxY);
        newAgent.SetNewGoal(agent_goal.x, agent_goal.y);
        newAgent.especial(SpecialMat);
        SetEscenarioAgent(newAgent);
        AddAgent(newAgent);

        Debug.Log("Creo nuevo agente especial");
    }

    public void CreateAgentInMiDirection()
    {
        CreatePositionAgentInMiDirection();
        float y_r = UnityEngine.Random.Range(0f, 360f);
        Quaternion v_r = Quaternion.Euler(0, y_r, 0);

        Vector3 v_p = new Vector3(x_p, 0.0f, z_p);

        agentID = RVO.Simulator.Instance.addAgent(new RVO.Vector2(x_p, z_p));

        Agent newAgent = Instantiate(prefab, v_p, v_r, parent.transform).GetComponent<Agent>();
        newAgent.SetID(agentID);
        newAgent.SetMaximumDiameter(maxX, maxY);
        SetInitialCoordinates(x_p, z_p, newAgent);
        SetEscenarioAgent(newAgent);
        newAgent.DirectionAsSpecial();
        AddAgent(newAgent);
    }

    private void CheckInitializedAgents(float x, float z)
    {
        bool exists = false;
        foreach (Vector2 v in PositionsInitializedAgents)
        {
            if (v.x == x && v.y == z) exists = true;
        }

        if (exists) CreateRandomPosition(); 
        else
        {
            PositionsInitializedAgents.Add(new Vector2(x, z));
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
        Agentes.Remove(a);
    }

    private void CreateObstacles()
    {
        GameObject[] _obstaculos = GameObject.FindGameObjectsWithTag("Pared");
        int i = 0;
        foreach (GameObject o in _obstaculos)
        {
            Vector3[] vertices = o.gameObject.GetComponent<MeshFilter>().mesh.vertices;
            List<RVO.Vector2> obstaculo = new List<RVO.Vector2>();
            foreach (Vector3 v in vertices)
            {
                obstaculo.Add(new RVO.Vector2(v.x, v.z));
            }
            RVO.Simulator.Instance.addObstacle(obstaculo);
            ++i;
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
            //Pasillo 1
            x = UnityEngine.Random.Range((maxX - 5f), (maxX - radius)); // 15, 19.5
            z = UnityEngine.Random.Range(-(maxY - radius), (maxY - radius)); // -4.5, 4.5
        }

        else if (aux % 4 == 1)
        {
            //Pasillo 2
            x = UnityEngine.Random.Range(-(maxX - radius),- (maxX - 5f)); //-19.5, -15
            z = UnityEngine.Random.Range(-(maxY - radius), (maxY - radius)); // -4.5, 4.5
        }
        else if (aux % 4 == 2)
        {
            //Pasillo 3
            z = UnityEngine.Random.Range((maxX - 5f), (maxX - radius)); // 15, 19.5
            x = UnityEngine.Random.Range(-(maxY - radius), (maxY - radius)); // -4.5, 4.5
        }
        else if (aux % 4 == 3)
        {
            //Pasillo 4
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
        foreach (Vector2 v in PositionsInitializedAgents)
        {
            if (v.x == x && v.y == z) exists = true;
        }

        if (exists) CreatePositionAgentInMiDirection();
        else
        {
            PositionsInitializedAgents.Add(new Vector2(x, z));
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

    private Vector2 CreateAgentGoal()
    {
        //Aqui deberia añadir en funcion de los escenarios
        float x_goal = (maxX);
        float z_goal = UnityEngine.Random.Range(-(maxY - 0.5f), (maxY - 0.5f));

        GameObject goal_prueba = new GameObject("goal");
        goal_prueba.transform.position = new Vector3(x_goal, 0, z_goal);

        return new Vector2(x_goal, z_goal);
    }
}
