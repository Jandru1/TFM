using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Simulator_mine : MonoBehaviour
{
    List<Agent> Agentes = new List<Agent>();
    public static Simulator_mine _instance = null;

    // Start is called before the first frame update
    void Start()
    {
        GetInstance();
    }

    // Update is called once per frame
    void Update()
    {
    }
    public static Simulator_mine GetInstance()
    {
        if(_instance == null)
        {
            GameObject SimGameObject = new GameObject("Simulator");
            _instance = SimGameObject.AddComponent<Simulator_mine>();
        }
        return _instance;
    }

    public void AddAgent(Agent agent)
    {
        Agentes.Add(agent);
    }
    public List<Agent> GetAgents()
    {
        return Agentes;
    }

}
