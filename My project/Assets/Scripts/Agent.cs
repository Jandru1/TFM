using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
    public float radius = 0.5f;
   // public Vector3 velocity;
    public int id;
    float treshold = 1f;
    private RVO_Crowd_Generator rvo;
    float x_goal;
    float z_goal;
    public Vector3 goal;
    float distance;
    Vector2 MyInitialCoordinates;
    private string escenario;

    private bool SameDirectionAsSpecial = false;

    float maxX, maxY;

    private bool SoyEspecial = false;

    GameObject CrowdCreator;
    RVO_Crowd_Generator my_rvo;

    private float maxSpeed;

    GameObject goal_prueba;

    // Start is called before the first frame update
    void Start()
    {
        //    newAgent.SetMaximumDiameter(maxX, maxY);
        //   SetInitialCoordinates(x,z, newAgent);
        //     Debug.Log("Las coordanas deberian ser " + x + " " + z);
    //    CreateNewGoal();
      //  gameObject.transform.position = MyInitialCoordinates;
    }

    private void Awake()
    {
        CrowdCreator = GameObject.FindGameObjectWithTag("CrowdCreatorObject");
        my_rvo = CrowdCreator.GetComponent<RVO_Crowd_Generator>();
        goal_prueba = new GameObject("goal");

    }

    // Update is called once per frame
    void Update()
    {
        if(escenario == "Salon")
        {
            distance = Vector3.Distance(gameObject.transform.position, goal_prueba.transform.position);

            Debug.Log("distance = " + distance);
            if (distance <= treshold)
            {
                CreateNewGoal();
            }
        }
    }

    void CreateNewGoal()
    {
        if(escenario == "Salon")
        {
            x_goal = Random.Range(-(maxX - 1), (maxX - 1));
            z_goal = Random.Range(-(maxY - 1), (maxY - 1));
        }

        else if (escenario == "Pasillo")
        {
            if(SoyEspecial || SameDirectionAsSpecial)
            {
                x_goal = (maxX);
                z_goal = Random.Range(-(maxY - 0.5f), (maxY - 0.5f));
                //Creo el objeto POSICION INICIAL
            //    GameObject posicion_inicial_GO = new GameObject("Posicion Inicial");
              //  Debug.Log("Pero son " + MyInitialCoordinates.x + " " + MyInitialCoordinates.y);
            //    posicion_inicial_GO.transform.position = new Vector3(MyInitialCoordinates.x, 0, MyInitialCoordinates.y);
            }
            else
            {
                x_goal = -(maxX);
                z_goal = Random.Range(-(maxY - 0.5f), (maxY - 0.5f));
            }
        }
        else if (escenario == "Cruce")
        {
            //GameObject CrowdCreator = GameObject.FindGameObjectWithTag("CrowdCreatorObject");
            //RVO_Crowd_Generator my_rvo = CrowdCreator.GetComponent<RVO_Crowd_Generator>();
            float x = MyInitialCoordinates.x;
            float z = MyInitialCoordinates.y;

            if (x > -(maxX-radius) && x < -(maxX - 5f)) // -19.5, -15
            {
                //Pasillo 1
                x_goal = maxX; // 15, 19.5
                z_goal = Random.Range(-(maxY - radius), (maxY - radius)); // -4.5, 4.5
            }

            else if (x > (maxX-5f) && x < (maxX - 0.5f)) // 15, 19.5
            {
                //Pasillo 2
                x_goal = -maxX; //-19.5, -15
                z_goal = Random.Range(-(maxY - radius), (maxY - radius)); // -4.5, 4.5
            }
            else if (z > -(maxX - radius) && z < -(maxX - 5f)) //-19.5, -15
            {
                //Pasillo 3
                z_goal = maxX; // 15, 19.5
                x_goal = Random.Range(-(maxY - radius), (maxY - radius)); // -4.5, 4.5
            }
            else if (z > (maxX - 5f) && x < (maxX - 0.5f)) // 15, 19.5
            {
                //Pasillo 4
                z_goal = -maxX;//-19.5, -15
                x_goal = Random.Range(-(maxY - radius), (maxY - radius));// -4.5, 4.5
            }
        }

        goal = new Vector3(x_goal, 0.0f, z_goal);

        goal_prueba.transform.position = goal;

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Pared")
        {
            if(escenario == "Pasillo" || escenario == "Cruce")
            {
                Debug.Log("PASO 8: Elimino Agente porque colisiono");
                my_rvo.RemoveAgent(this);
               // Destroy(gameObject.transform.parent.gameObject);
                if (SoyEspecial) my_rvo.CreateSpecialAgent();
                else if (SameDirectionAsSpecial)my_rvo.CreateAgentInMiDirection();
                else my_rvo.CreateAgent();
                this.gameObject.transform.parent.gameObject.SetActive(false);
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
    }

    public virtual void SetPreferredVelocity()
    {
        Vector2 goal_prueba_POS = new Vector2(goal_prueba.transform.position.x, goal_prueba.transform.position.z);
        RVO.Vector2 goalVector = new RVO.Vector2(goal_prueba_POS.x, goal_prueba_POS.y) - RVO.Simulator.Instance.getAgentPosition(id);

        Debug.Log("Posicion actual de Joe " + transform.position);
        Debug.Log("Posición actual del rvo agente = " + RVO.Simulator.Instance.getAgentPosition(id));
        Debug.Log("goalVector = " + goalVector);

        if (RVO.RVOMath.absSq(goalVector) > 1.0f)
        {
            goalVector = RVO.RVOMath.normalize(goalVector);
        }

        Debug.Log("Maxspeeed" + maxSpeed);
        RVO.Simulator.Instance.setAgentPrefVelocity(id, goalVector*maxSpeed);
    }

    public virtual void UpdatePos()
    {
        //Option1();
        Option2();
       
    }

    void Option1()
    {
        RVO.Vector2 pos = RVO.Simulator.Instance.getAgentPosition(id);
      //  gameObject.transform.position = new Vector3(pos.x(), 0.0f, pos.y());
    }

    void Option2()
    {
        Vector3 v = new Vector3(RVO.Simulator.Instance.getAgentVelocity(id).x_, 0.0f, RVO.Simulator.Instance.getAgentVelocity(id).y_);
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
      //  rb.MovePosition(rb.position + (v*(Time.deltaTime)));
      //  RVO.Simulator.Instance.setAgentPosition(id, new RVO.Vector2(rb.position.x, rb.position.z));
      //  RVO.Simulator.Instance.setAgentVelocity(id, new RVO.Vector2(v.x, v.z));
    }

    public void SetID(int _id)
    {
        id = _id;
    }

    public int GetID()
    {
        return id;
    }

    public void SetMaximumDiameter(float x, float y)
    {
        maxX = x;
        maxY = y;
    }

    public void SetEscenario(string _escenario)
    {
        escenario = _escenario;
    }

    public void especial(Material material)
    {
        SoyEspecial = true;
        gameObject.GetComponent<MeshRenderer>().material = material;
    }

    public void SetInitialCoordinates(float x, float z)
    {
        MyInitialCoordinates = new Vector2(x, z);
    }

    public void DirectionAsSpecial()
    {
        SameDirectionAsSpecial = true;
    }

    public Vector2 GetGoal()
    {
        Debug.Log("Goal prueba = " + goal_prueba.transform.position);
        return new Vector2(goal_prueba.transform.position.x, goal_prueba.transform.position.z);
    }

    public Vector2 GetInitPosition()
    {
        return new Vector2(MyInitialCoordinates.x, MyInitialCoordinates.y);
    }

    public void SetNewGoal(float x, float z)
    {
        x_goal = x;
        z_goal = z;
        goal = new Vector3(x_goal, 0, z_goal);

        goal_prueba.transform.position = goal;
    }

    public void setMaxSpeed(float speed)
    {
        maxSpeed = speed;
    }

}
