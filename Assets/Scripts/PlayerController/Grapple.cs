using System.Collections;
using System.Linq;
using MoreLinq;
using System.Collections.Generic;
using UnityEngine;

public class GrappleSite
{
    public Vector3 position;
    public bool calculatedAngle = false;
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
    List<GrappleSite> grappleSites = new List<GrappleSite>();
    float distanceExtended = 0;
    float oldgrav;

    float connectionThreshold = .05f;
    float veryShortLineThreshold = .05f;

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
        if (currentState == States.Extending)
        {
            grappleSites[0].position = body.transform.position;
            var headgrapple = grappleSites[1];
            var backgrapple = grappleSites[0];
            var direction = headgrapple.position - backgrapple.position;
            float velocityTowardsHook = Vector3.Dot(body.velocity, direction);

            var delta = (extensionDirection * characterController.grappleSpeed) * Time.deltaTime;
            headgrapple.position += delta + startingVelocity * Time.deltaTime;
            distanceExtended += delta.magnitude;

            if (distanceExtended >= characterController.grappleDistance)
            {
                scram();
                return;
            }

            backgrapple = grappleSites[grappleSites.Count - 2];
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
                    break;
                }
            }
        }
        if (currentState == States.Stuck)
        {
            var oldsites = grappleSites.Select(g => new Vector3(g.position.x, g.position.y, g.position.z)).ToList();

            grappleSites[0].position = body.transform.position;
            var headgrapple = grappleSites[1];
            var backgrapple = grappleSites[0];
            var direction = headgrapple.position - backgrapple.position;
            float velocityTowardsHook = Vector3.Dot(body.velocity, direction);

            if (characterController.bendGrapple)
            {
                grappleSites = recheckGrapplePoints(oldsites);
                grappleSites = cleanGrapples();
            }
        }


        getRope().SetPositions(grappleSites.Select(g => g.position).ToArray());


    }

    void FixedUpdate ()
    {
        if (currentState == States.Stuck)
        {
            
            var headgrapple = grappleSites[1];
            var backgrapple = grappleSites[0];
            var direction = headgrapple.position - backgrapple.position;

            var force = direction.normalized * (characterController.grappleForce + direction.magnitude * characterController.grappleForceDistanceBoost);
            characterController.applyContinuousForce(force, characterController.grappleMaxVelocity);


            getRope().numPositions = grappleSites.Count;
            getRope().SetPositions(grappleSites.Select(g => g.position).ToArray());

        }

    }

    public List<GrappleSite> cleanGrapples()
    {
        var newGrapplePos = new List<GrappleSite>();
        newGrapplePos.Add(grappleSites[0]);
        for (int i = 1; i < grappleSites.Count - 1; i++) // we want all the elements of the list but the first and last
        {

            var prevGrap = grappleSites[i-1];
            var currentGrap = grappleSites[i];
            var nextGrap = grappleSites[i + 1];
            if (!prevGrap.position.Colinear(currentGrap.position, nextGrap.position))
            {
                var angle = threePointAngle(prevGrap.position, currentGrap.position, nextGrap.position);
                if (currentGrap.calculatedAngle)
                {
                    Debug.Log(i + ": " + angle + " vs the old angle " + currentGrap.angle);
                    if (angle / currentGrap.angle > 0) // if the angle has not changed signs
                    {
                        currentGrap.angle = angle;
                        newGrapplePos.Add(currentGrap);
                    }
                }
                else
                {
                    currentGrap.angle = angle;
                    currentGrap.calculatedAngle = true;
                    newGrapplePos.Add(currentGrap);
                }
            }

        }
        newGrapplePos.Add(grappleSites.Last());
        return newGrapplePos;
    }

    public List<GrappleSite> recheckGrapplePoints(List<Vector3> oldSites)
    {
        var newGrapplePos = new List<GrappleSite>();
        newGrapplePos.Add(grappleSites[0]);
        for (int i = 0; i < grappleSites.Count - 1; i++)
        {

            var firstGrap = grappleSites[i];
            var secondGrap = grappleSites[i + 1];

            var dir = secondGrap.position - firstGrap.position;

            if ((firstGrap.position != oldSites[i] || secondGrap.position != oldSites[i+1]) && (firstGrap.position - secondGrap.position).sqrMagnitude > Mathf.Pow(veryShortLineThreshold, 2))
            {
                var hits = Physics2D.RaycastAll(firstGrap.position, dir, dir.magnitude - connectionThreshold, characterController.grappleBendMask);
                var hit = hits.FirstOrDefault();
                if (hit && hit.collider.gameObject != body.gameObject)
                {
                    var hitpoint = getNearestColliderPoint(hit);
                    if (hitpoint != newGrapplePos.Last().position)
                    {
                        if (characterController.useAdvancedBending)
                        {
                            var verticies = hits.SelectMany(h => getAllColliderPoints(h)).Select(v => (Vector2)v).ToArray();
                            foreach (var v in verticies)
                            {
                                Debug.DrawRay(v, Vector3.up * .05f, Color.red);
                                Debug.DrawRay(v, Vector3.left * .05f, Color.red);
                                Debug.DrawRay(v, Vector3.right * .05f, Color.red);
                                Debug.DrawRay(v, Vector3.down * .05f, Color.red);
                            }
                            newGrapplePos.AddRange(sweepAndBend(secondGrap.position, oldSites[i], firstGrap.position, verticies).Select(v => new GrappleSite(v)));
                        }
                        else
                        {
                            var gSite = new GrappleSite(hitpoint);
                            newGrapplePos.Add(gSite);
                        }
                    }
                }
            }

            newGrapplePos.Add(secondGrap);
        }
        return newGrapplePos;
    }

    public List<Vector3> sweepAndBend(Vector3 pivot, Vector3 oldPosition, Vector3 newPosition, Vector2[] verticies)
    {
        var verticiesInTriangle = verticies.Where(v => pointWithinTriangle(v, pivot, oldPosition, newPosition)).ToList();
        if (verticiesInTriangle.Count() == 0) return new List<Vector3>() { };

        var newPivot = verticiesInTriangle.MaxBy(v => Mathf.Abs(threePointAngle(v, pivot, newPosition)));
        
        return new List<Vector3>(sweepAndBend(newPivot, oldPosition, newPosition, verticiesInTriangle.ToArray())) { newPivot }; ;
    }

    public bool pointWithinTriangle(Vector3 point, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float alpha = ((p2.y - p3.y) * (point.x - p3.x) + (p3.x - p2.x) * (point.y - p3.y)) /
                ((p2.y - p3.y) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.y - p3.y));
        float beta = ((p3.y - p1.y) * (point.x - p3.x) + (p1.x - p3.x) * (point.y - p3.y)) /
                ((p2.y - p3.y) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.y - p3.y));
        float gamma = 1.0f - alpha - beta;

        return alpha > 0 && beta > 0 && gamma > 0;

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
        grappleSites.Add(new GrappleSite(body.transform.position)); // current starting position of rope
        grappleSites.Add(new GrappleSite(body.transform.position)); // current ending position of rope
        currentState = States.Extending;

        getRope().numPositions = grappleSites.Count;
        getRope().SetPositions(grappleSites.Select(g => g.position).ToArray());

    }

    public float threePointAngle(Vector3 A, Vector3 B, Vector3 C)
    {

        var Va = A - B;
        var Vb = C - B;

        var angle = Mathf.Acos(Vector3.Dot(Va.normalized, Vb.normalized));
        var cross = Vector3.Cross(Va, Vb);
        if (Vector3.Dot(Vector3.forward, cross) < 0)
        { // Or > 0
            angle = -angle;
        }
        return angle;
    }

    private List<Vector3> getAllColliderPoints(RaycastHit2D hit)
    {
        var verticies = new List<Vector3>();
        if (hit.collider as PolygonCollider2D != null)
        {
            PolygonCollider2D collider = hit.collider as PolygonCollider2D;
            for (var pathIndex = 0; pathIndex < collider.pathCount; pathIndex++)
                foreach (Vector3 colliderPoint in collider.GetPath(pathIndex))
                    // Convert to world point
                    verticies.Add(collider.transform.TransformPoint(colliderPoint + (Vector3)collider.offset));
        }
        else if (hit.collider as BoxCollider2D != null)
        {
            BoxCollider2D collider = hit.collider as BoxCollider2D;
            Vector2 size = collider.size;
            Vector3 centerPoint = new Vector3(collider.offset.x, collider.offset.y, 0f);
            Vector3 worldPos = collider.transform.TransformPoint(centerPoint);

            float top = worldPos.y + (size.y / 2f);
            float btm = worldPos.y - (size.y / 2f);
            float left = worldPos.x - (size.x / 2f);
            float right = worldPos.x + (size.x / 2f);

            verticies.Add(new Vector3(left, top, worldPos.z));
            verticies.Add(new Vector3(right, top, worldPos.z));
            verticies.Add(new Vector3(left, btm, worldPos.z));
            verticies.Add(new Vector3(right, btm, worldPos.z));
        }
        else if (hit.collider as EdgeCollider2D != null) // Untested!
        {
            EdgeCollider2D collider = hit.collider as EdgeCollider2D;
            foreach (var p in collider.points)
                verticies.Add(collider.transform.TransformPoint(p + collider.offset));
        }

        return verticies;
    }

    private Vector3 getNearestColliderPoint(RaycastHit2D hit)
    {

        float minDistanceSqr = Mathf.Infinity;
        Vector3 nearestColliderPoint = Vector3.zero;

        foreach (Vector3 colliderPoint in getAllColliderPoints(hit)) 
        {
            // Convert to world point

            Vector3 diff = hit.point - (Vector2)colliderPoint;
            float distSqr = diff.sqrMagnitude;

            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                nearestColliderPoint = colliderPoint;
            }
        }

        return nearestColliderPoint;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        foreach (var site in grappleSites)
        {
            Gizmos.DrawSphere(site.position, .05f);
        }
    }

}
