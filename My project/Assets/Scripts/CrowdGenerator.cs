using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CrowdGenerator : MonoBehaviour
{
    public GameObject prefab;
    public GameObject parent;

    public float agents_number;
    // Start is called before the first frame update
    void Start()
    {
        for(int i = 0; i < agents_number; ++i)
        {
            float x_p = Random.Range(-10, 10);
            float z_p = Random.Range(-10, 10);
            Vector3 v_p = new Vector3(x_p, 1, z_p);
            float y_r = Random.Range(0f, 360f);
            
            Quaternion v_r = Quaternion.Euler(0, y_r, 0);

            Agent newAgent = Instantiate(prefab, v_p, v_r, parent.gameObject.transform).GetComponent<Agent>();
            //NavMeshAgent agente = Instantiate(prefab, v_p, v_r, parent.gameObject.transform).GetComponent<NavMeshAgent>();
            // agente.destination = goal;

             AddAgent(newAgent);
           // RVO.Simulator.Instance.addAgent(newAgent);
        }

    }

    // Update is called once per frame
    void Update()
    {
    }

    void AddAgent(Agent a)
    {
        GameObject simulator = GameObject.Find("Simulator");
        simulator.GetComponent<Simulator_mine>().AddAgent(a);
    }

}
