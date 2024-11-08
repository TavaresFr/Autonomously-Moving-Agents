using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Bot : MonoBehaviour
{
    NavMeshAgent agent;
    [SerializeField] GameObject target;
    Drive ds;

    [SerializeField] float wanderRadius = 10;

    [SerializeField] float wanderDistance = 20;

    [SerializeField] float wanderJitter = 2;

    // Start is called before the first frame update
    void Start()
    {
        agent = this.GetComponent<NavMeshAgent>();
        ds = target.AddComponent<Drive>();
    }

    void Seek(Vector3 location)
    {
        agent.SetDestination(location);
    }

    void Flee(Vector3 location)
    {
        Vector3 fleeVector = location - this.transform.position;
        agent.SetDestination(this.transform.position - fleeVector);
    }

    void Pursue()
    {
        Vector3 targetDir = target.transform.position - this.transform.position;

        float relativeHeading = Vector3.Angle(this.transform.forward, this.transform.TransformVector(target.transform.forward));
        float toTarget = Vector3.Angle(this.transform.forward, this.transform.TransformVector(targetDir));

        if ((relativeHeading < 20 && toTarget > 90) || ds.currentSpeed < 0.01f)
        {
            Seek(target.transform.position);
            return;
        }

        float lookAhead = targetDir.magnitude/(agent.speed + ds.currentSpeed);

        Seek(target.transform.position + target.transform.forward * lookAhead);
    }

    void Evade()
    {
        Vector3 targetDir = target.transform.position - this.transform.position;

        float lookAhead = targetDir.magnitude / (agent.speed + ds.currentSpeed);

        Flee(target.transform.position + target.transform.forward * lookAhead);
    }

    Vector3 wanderTarget = Vector3.zero;

    void Wander()
    {
        //when we do this the wander target is off the wander radius
        wanderTarget += new Vector3(Random.Range(-1.0f, 1.0f) * wanderJitter, 0, Random.Range(-1.0f, 1.0f) * wanderJitter);

        //so then we normalize it which will put it to values of 1 etc..
        wanderTarget.Normalize();

        //and then we multiply it by the wander radius that puts it again on the edge of the wander radius (imagine a circle)
        wanderTarget *= wanderRadius;

        Vector3 targetLocal = wanderTarget + new Vector3(0, 0, wanderDistance);
        Vector3 targetWorld = this.gameObject.transform.InverseTransformVector(targetLocal);

        Seek(targetWorld);
    }

    void Hide()
    {
        float dist = Mathf.Infinity;
        Vector3 chosenSpot = Vector3.zero;

        for(int i = 0; i < World.Instance.GetHidingSpots().Length; i++)
        {
            Vector3 hideDir = World.Instance.GetHidingSpots()[i].transform.position - target.transform.position;
            Vector3 hidePosition = World.Instance.GetHidingSpots()[i].transform.position + hideDir.normalized * 5;

            if(Vector3.Distance(this.transform.position, hidePosition) < dist)
            {
                chosenSpot = hidePosition;
                dist = Vector3.Distance(this.transform.position, hidePosition);
            }
        }

        Seek(chosenSpot);
    }

    void CleverHide()
    {
        float dist = Mathf.Infinity;
        Vector3 chosenSpot = Vector3.zero;
        Vector3 chosenDir = Vector3.zero;
        GameObject chosenGO = World.Instance.GetHidingSpots()[0];

        for (int i = 0; i < World.Instance.GetHidingSpots().Length; i++)
        {
            Vector3 hideDir = World.Instance.GetHidingSpots()[i].transform.position - target.transform.position;
            Vector3 hidePosition = World.Instance.GetHidingSpots()[i].transform.position + hideDir.normalized * 5;

            if (Vector3.Distance(this.transform.position, hidePosition) < dist)
            {
                chosenSpot = hidePosition;
                chosenDir = hideDir;
                chosenGO = World.Instance.GetHidingSpots()[i];
                dist = Vector3.Distance(this.transform.position, hidePosition);
            }
        }

        Collider hideCol = chosenGO.GetComponent<Collider>();

        Ray backRay = new Ray(chosenSpot, -chosenDir.normalized);

        RaycastHit info;

        float distance = 100.0f;

        hideCol.Raycast(backRay, out info, distance);

        Seek(info.point + chosenDir.normalized * 5);
    }

    bool CanSeeTarget()
    {
        RaycastHit raycastInfo;
        Vector3 rayToTarget = target.transform.position - this.transform.position;

        float lookAngle = Vector3.Angle(this.transform.forward, rayToTarget);

        if(lookAngle < 60 && Physics.Raycast(this.transform.position, rayToTarget, out raycastInfo))
        {
            if (raycastInfo.transform.gameObject.tag == "cop")
                return true;
        }

        return false;
    }

    bool CanSeeMe()
    {
        Vector3 targetVector = this.transform.position - target.transform.position;

        float lookAngle = Vector3.Angle(target.transform.forward, targetVector);

        if(lookAngle < 60)
            return true;

        return false;
    }

    bool coolDown = false;

    void BehaviourCooldown()
    {
        coolDown = false;
    }

    bool TargetInRange()
    {
        if(Vector3.Distance(this.transform.position, target.transform.position) < 10)
        {
            return true;
        }

        return false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!coolDown)
        {
            if (!TargetInRange())
            {
                Wander();
            }
            else if (CanSeeTarget() && CanSeeMe())
            {
                CleverHide();
                coolDown = true;
                Invoke("BehaviourCooldown", 5);
            }
            else
                Pursue();
        }
    }
}
