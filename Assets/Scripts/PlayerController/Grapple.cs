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
        if (currentState == States.Extending)
        {
            var delta = (dir * speed) * Time.deltaTime;
            grapplePos[grapplePos.Count - 1].position += delta + startingVelocity * Time.deltaTime;
            distanceExtended += delta.magnitude;
            getRope().SetPositions(grapplePos.Select(g => g.position).ToArray());

            if (distanceExtended >= maxExtent)
                Destroy(gameObject);

            //var hit = Physics2D.Raycast(grapplePos[grapplePos.Count - 2], -Vector2.up);
        }

        


	}

    public void shoot(Rigidbody2D shooter, Vector3 shootDirection, float fireSpeed, float maxExtension)
    {
        owner = shooter;
        dir = shootDirection.normalized;
        speed = fireSpeed;
        startingVelocity = shooter.velocity;
        maxExtent = maxExtension;
        grapplePos.Add(new GrappleSite(shooter.position)); // current starting position of rope
        grapplePos.Add(new GrappleSite(shooter.position)); // current ending position of rope

        currentState = States.Extending;

        getRope().numPositions = grapplePos.Count;
        getRope().SetPositions(grapplePos.Select(g => g.position).ToArray());

    }
}
