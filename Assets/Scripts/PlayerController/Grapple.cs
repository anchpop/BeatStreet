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

    Rigidbody2D body;
    CharacterControllerPlatformer characterController;
    Vector3 startingVelocity;
    Vector3 extensionDirection;
    List<GrappleSite> grapplePos = new List<GrappleSite>();
    float distanceExtended = 0;
    float oldgrav;

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


    private void Update()
    {
        grapplePos[0].position = body.transform.position;
        var headgrapple = grapplePos[grapplePos.Count - 1];
        var backgrapple = grapplePos[grapplePos.Count - 2];
        var direction = headgrapple.position - backgrapple.position;
        float velocityTowardsHook = Vector3.Dot(body.velocity, direction);

        if (currentState == States.Extending)
        {

            var delta = (extensionDirection * characterController.grappleSpeed) * Time.deltaTime;
            headgrapple.position += delta + startingVelocity * Time.deltaTime;
            distanceExtended += delta.magnitude;

            if (distanceExtended >= characterController.grappleDistance)
            {
                scram();
                return;
            }

            backgrapple = grapplePos[grapplePos.Count - 2];
            direction = headgrapple.position - backgrapple.position;

            var hits = Physics2D.RaycastAll(backgrapple.position, direction, direction.magnitude, characterController.grappleMask);
            foreach (var hit in hits)
            {
                if (hit && hit.collider.gameObject != body.gameObject)
                {
                    currentState = States.Stuck;
                    oldgrav = body.gravityScale;
                    body.gravityScale = characterController.grappleGravityScale;
                    return;
                }
            }
        }


        getRope().SetPositions(grapplePos.Select(g => g.position).ToArray());


    }

    void FixedUpdate ()
    {
        grapplePos[0].position = body.transform.position;
        var headgrapple = grapplePos[grapplePos.Count - 1];
        var backgrapple = grapplePos[grapplePos.Count - 2];
        var direction = headgrapple.position - backgrapple.position;
        float velocityTowardsHook = Vector3.Dot(body.velocity, direction);

        if (currentState == States.Stuck)
        {
            var force = direction.normalized * (characterController.grappleForce + direction.magnitude * characterController.grappleForceDistanceBoost);
            characterController.applyContinuousForce(force, characterController.grappleMaxVelocity);
        }


        getRope().SetPositions(grapplePos.Select(g => g.position).ToArray());

    }

    public void scram()
    {
        body.gravityScale = oldgrav;
        Destroy(gameObject);
    }

    public void shoot(CharacterControllerPlatformer shooter, Vector3 shootDirection)
    {
        body = shooter.GetComponent<Rigidbody2D>();
        characterController = shooter;
        extensionDirection = shootDirection.normalized;
        startingVelocity = body.velocity;
        grapplePos.Add(new GrappleSite(body.position)); // current starting position of rope
        grapplePos.Add(new GrappleSite(body.position)); // current ending position of rope
        currentState = States.Extending;

        getRope().numPositions = grapplePos.Count;
        getRope().SetPositions(grapplePos.Select(g => g.position).ToArray());

    }
}
