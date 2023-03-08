using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshController : MonoBehaviour
{

    public Transform obj;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        NavMeshAgent agente = GetComponent<NavMeshAgent>();
        agente.destination = obj.position;
    }
}
