using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class WanderAround : MonoBehaviour
{
    public NavMeshAgent Agent;

    public float WanderDistance = 10f;
    void Start()
    {
        if (Agent == null)
        {
            Agent = GetComponent<NavMeshAgent>();
        }
    }
    void Update ()
    {
        if (Agent.remainingDistance <= Agent.stoppingDistance)
        {
            Agent.SetDestination(Agent.RandomPosition(WanderDistance));
        }
    }
}

public static class NavMeshExtensions
{
    public static Vector3 RandomPosition(this NavMeshAgent agent, float radius)
    {
        var randDirection = UnityEngine.Random.insideUnitSphere * radius;
        randDirection += agent.transform.position;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, radius, -1);
        return navHit.position;
    }
}