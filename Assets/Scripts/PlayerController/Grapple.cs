using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class GrappleSite
{
    public Vector3 position;
    public float angle;

    public GrappleSite(Vector3 p)
    {
        position = p;
    }
}

public class Grapple : MonoBehaviour {
    enum States
    {
        Limbo,
        Extending,
        Stuck
    }

    Rigidbody2D owner;
    float speed;
    Vector3 startingVelocity;
    Vector3 dir;
    List<GrappleSite> grapplePos = new List<GrappleSite>();
    float maxExtent;
    float distanceExtended = 0;
    float baseForce;
    float forceMultiplier;
    float gravityScale;
    float oldgrav;

    LayerMask grappleMask;

    float bendThreshold = .05f;

    States currentState = States.Limbo;

    LineRenderer rope;
	
	void Start ()
    {
	}

    LineRenderer getRope()
    {
        if (GetComponent<LineRenderer>())
            return GetComponent<LineRenderer>();
        else
        {
            rope = gameObject.AddComponent<LineRenderer>();
            rope.material = new Material(Shader.Find("Particles/Additive"));

            return rope;
        }
    }
	
	
	void Update ()
    {
        grapplePos[0].position = owner.transform.position;
        var headgrapple = grapplePos[grapplePos.Count - 1];
        var backgrapple = grapplePos[grapplePos.Count - 2];
        var direction = headgrapple.position - backgrapple.position;

        if (currentState == States.Extending)
        {

            var delta = (dir * speed) * Time.deltaTime;
            headgrapple.position += delta + startingVelocity * Time.deltaTime;
            distanceExtended += delta.magnitude;

            if (distanceExtended >= maxExtent)
            {
                scram();
                return;
            }

            backgrapple = grapplePos[grapplePos.Count - 2];
            direction = headgrapple.position - backgrapple.position;

            var hits = Physics2D.RaycastAll(backgrapple.position, direction, direction.magnitude, grappleMask);
            foreach (var hit in hits)
            {
                if (hit && hit.collider.gameObject != owner.gameObject)
                {
                    currentState = States.Stuck;
                    oldgrav = owner.gravityScale;
                    owner.gravityScale = gravityScale;
                    return;
                }
            }
        }
        if (currentState == States.Stuck)
        {
            owner.AddForce(direction.normalized * (baseForce + direction.magnitude * forceMultiplier), ForceMode.VelocityChange);
        }


        getRope().SetPositions(grapplePos.Select(g => g.position).ToArray());

    }

    public void scram()
    {
        owner.gravityScale = oldgrav;
        Destroy(gameObject);
    }

    public void shoot(Rigidbody2D shooter, Vector3 shootDirection, float fireSpeed, float maxExtension, float grappleForce, float grappleForceMultiplier, float grappleGravityScale, LayerMask mask)
    {
        owner = shooter;
        dir = shootDirection.normalized;
        speed = fireSpeed;
        startingVelocity = shooter.velocity;
        maxExtent = maxExtension;
        grappleMask = mask;
        baseForce = grappleForce;
        forceMultiplier = grappleForceMultiplier;
        grapplePos.Add(new GrappleSite(shooter.position)); // current starting position of rope
        grapplePos.Add(new GrappleSite(shooter.position)); // current ending position of rope
        gravityScale = grappleGravityScale;
        currentState = States.Extending;

        getRope().numPositions = grapplePos.Count;
        getRope().SetPositions(grapplePos.Select(g => g.position).ToArray());

    }
}
