using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class GrappleSite
{
    public Vector3 position;
    public float angle;
    GameObject connected;

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

    float connectionThreshold = .05f;

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

            var hits = Physics2D.LinecastAll(backgrapple.position, headgrapple.position, characterController.grappleMask);
            foreach (var hit in hits)
            {
                if (hit && hit.collider.gameObject != body.gameObject)
                {
                    currentState = States.Stuck;
                    oldgrav = body.gravityScale;
                    body.gravityScale = characterController.grappleGravityScale;
                    headgrapple.position = hit.point;
                    return;
                }
            }
        }


        getRope().SetPositions(grapplePos.Select(g => g.position).ToArray());


    }

    void FixedUpdate ()
    {
        grapplePos[0].position = body.transform.position;
        var headgrapple = grapplePos[1];
        var backgrapple = grapplePos[0];
        var direction = headgrapple.position - backgrapple.position;
        float velocityTowardsHook = Vector3.Dot(body.velocity, direction);

        if (currentState == States.Stuck)
        {
            var force = direction.normalized * (characterController.grappleForce + direction.magnitude * characterController.grappleForceDistanceBoost);
            characterController.applyContinuousForce(force, characterController.grappleMaxVelocity);

            if (characterController.bendGrapple)
            {

                grapplePos = cleanGrapples();
                grapplePos = recheckGrapplePoints();
            }
        }

        getRope().numPositions = grapplePos.Count;
        getRope().SetPositions(grapplePos.Select(g => g.position).ToArray());

    }

    public List<GrappleSite> cleanGrapples()
    {
        var newGrapplePos = new List<GrappleSite>();
        newGrapplePos.Add(grapplePos[0]);
        for (int i = 1; i < grapplePos.Count - 1; i++) // we want all the elements of the list but the first and last
        {

            var prevGrap = grapplePos[i-1];
            var currentGrap = grapplePos[i];
            var nextGrap = grapplePos[i + 1];
            var angle = (currentGrap.position - prevGrap.position).AngleTo(currentGrap.position - nextGrap.position);
            if (angle / currentGrap.angle < 0) newGrapplePos.Add(currentGrap);
        }
        newGrapplePos.Add(grapplePos.Last());
        return newGrapplePos;
    }

    public List<GrappleSite> recheckGrapplePoints()
    {
        var newGrapplePos = new List<GrappleSite>();
        newGrapplePos.Add(grapplePos[0]);
        for (int i = 0; i < grapplePos.Count - 1; i++)
        {

            var firstGrap = grapplePos[i];
            var secondGrap = grapplePos[i + 1];

            var dir = secondGrap.position - firstGrap.position;

            var hit = Physics2D.Raycast(firstGrap.position, dir, dir.magnitude - connectionThreshold, characterController.grappleBendMask);
            if (hit && hit.collider.gameObject != body.gameObject && hit.point != (Vector2)newGrapplePos.Last().position)
            {
                var gSite = new GrappleSite(hit.point);
                newGrapplePos.Add(gSite);
                newGrapplePos.Last().angle = (gSite.position - newGrapplePos.Last().position).AngleTo(secondGrap.position - newGrapplePos.Last().position);
            }

            newGrapplePos.Add(secondGrap);
        }
        return newGrapplePos;
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
