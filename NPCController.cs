using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Yarn.Unity;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCController : MonoBehaviour
{
    public enum NPCState { Idle, Wandering, Cutscene }
    
    [Header("Identity")]
    [SerializeField] private string npcID; // Matches name referenced in Yarn if needed
    public string NPCID => npcID;

    [Header("Current State")]
    [SerializeField] private NPCState currentState = NPCState.Idle;

    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float minWaitTime = 2f;
    [SerializeField] private float maxWaitTime = 5f;

    private NavMeshAgent agent;
    private Vector3 startPosition;
    private Coroutine behaviorRoutine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        startPosition = transform.position;
    }

    private void Start()
    {
        SetState(NPCState.Wandering);
    }

    public void SetState(NPCState newState)
    {
        currentState = newState;

        if (behaviorRoutine != null)
        {
            StopCoroutine(behaviorRoutine);
        }

        switch (currentState)
        {
            case NPCState.Idle:
                agent.isStopped = true;
                break;
            case NPCState.Wandering:
                agent.isStopped = false;
                behaviorRoutine = StartCoroutine(WanderRoutine());
                break;
            case NPCState.Cutscene:
                agent.isStopped = true;
                agent.ResetPath();
                break;
        }
    }

    private IEnumerator WanderRoutine()
    {
        while (currentState == NPCState.Wandering)
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += startPosition;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance);
            }

            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);
        }
    }

    // --- Timeline / Cutscene Hooks ---
    public void ForceMoveTo(Vector3 targetPosition, System.Action onComplete = null)
    {
        SetState(NPCState.Cutscene);
        agent.isStopped = false;
        agent.SetDestination(targetPosition);
        StartCoroutine(WaitForDestinationRoutine(onComplete));
    }

    private IEnumerator WaitForDestinationRoutine(System.Action onComplete)
    {
        yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance);
        agent.isStopped = true;
        onComplete?.Invoke();
    }
}