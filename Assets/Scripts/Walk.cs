using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class Walk : MonoBehaviour
{

    public Transform destination;

    private NavMeshAgent agent;

    void Start()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();

        agent.SetDestination(destination.position);
    }

}