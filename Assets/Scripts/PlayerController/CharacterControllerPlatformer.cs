using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;



[RequireComponent(typeof(Rigidbody2D))]
public class CharacterControllerPlatformer : MonoBehaviour {
    Rigidbody2D body;
    BoxCollider2D boxCol;

    [Header("Walking")]
    [Tooltip("The maximum velocity that will be attainable by walking")]
    public float maxRunVel = 3;
    [Tooltip("The force that will be applied to the player when he is walking")]
    public float runAccel = 5;     // acceleration when running 
    [Tooltip("The force that will be applied to the player when he is trying to walk in the opposite direction he is moving")]
    public float counterForce = 8; // acceleration when trying to move the opposite direction as the movement
    public float groundFriction = .9f;

    [Header("Jumping")]
    [Tooltip("The y velocity for the player to have after a jump")]
    public float jumpVelocity = 2;
    public int numberOfJumps = 1;
    int jumpsRemaining = 0;
    [Tooltip("Gravity when the player is moving upward and holding the up-button")]
    public float upwardGravityScale = .9f;
    [Tooltip("Gravity when the player is moving downward")]
    public float downwardGravityScale = 1.3f;
    public float airFriction = .3f;


    [Header("Grappling")]
    public float grappleSpeed = 3;
    public float grappleDistance = 7;
    public float grappleForce = 5;
    public float grappleForceDistanceBoost = 0;
    public float grappleMaxVelocity = 11;
    public float grappleGravityScale = .2f;
    public LayerMask grappleMask;

    [Header("Misc.")]
    public float maxXVel = 20;
    public float maxYVel = 50;
    public LayerMask groundLayer;

    bool jumpedThisFrame = false;
    bool walkedThisFrame = false;
    bool tryingToGoUp = false;
    float timeSinceLastJump;
    float minSecondsBetweenTrumps = .2f;

    float raycastDownDist = .1f;

    Grapple grapple;
    GameObject grappleObject;


	void Start ()
    {
        body = GetComponent<Rigidbody2D>();
        boxCol = GetComponent<BoxCollider2D>();

        timeSinceLastJump = Time.time;
    }

    public void applyContinuousForce(Vector3 force, float maxVelocity)
    {
        Vector2 velocityChange = force * Time.fixedDeltaTime;
        float velocityInDirection = Vector3.Dot(body.velocity, force.normalized);
        float velocityInDirectionAfterForce = Vector3.Dot(body.velocity + velocityChange, force.normalized);


        if (Mathf.Abs(velocityInDirectionAfterForce) < maxVelocity || Mathf.Sign(velocityInDirectionAfterForce) < 0) // if we're moving in the opposite direction of movement
            body.AddForce(force, ForceMode.Acceleration);
        else if (Mathf.Abs(velocityInDirection) < maxVelocity) // if the speed addition wouldn't make us accelerate past the max run speed
            body.velocity = body.velocity - (Vector2)Vector3.Project(body.velocity, force) + (Vector2)(force.normalized * maxVelocity);
        //else if (Mathf.Abs(body.velocity.x) <= maxRunVel)       // if we're moving slower than the max run speed already
        // body.velocity = new Vector2(Sign(body.velocity.x) * maxRunVel, body.velocity.y); // bring us straight to the maximum speed  

    }

    List<RaycastHit2D> raycastDown(float dist)
    {
        var bottomRight = boxCol.bounds.center + new Vector3(boxCol.bounds.extents.x, -boxCol.bounds.extents.y, 0);
        var bottomLeft  = boxCol.bounds.center + new Vector3(-boxCol.bounds.extents.x, -boxCol.bounds.extents.y, 0);
        List<RaycastHit2D> hits = new List<RaycastHit2D>();
        hits.Add(Physics2D.Raycast(bottomLeft, Vector3.down, dist, groundLayer));
        hits.Add(Physics2D.Raycast(bottomRight, Vector3.down, dist, groundLayer));
        return hits;
    }

    public void tryUp()
    {
        tryingToGoUp = true;
    }

    public void jump()
    {
        if (jumpsRemaining > 0 && !jumpedThisFrame && (Time.time - timeSinceLastJump) > minSecondsBetweenTrumps)
        {
            body.velocity = new Vector3(body.velocity.x, Mathf.Max(jumpVelocity, body.velocity.y), 0);
            jumpedThisFrame = true;
            jumpsRemaining--;
            timeSinceLastJump = Time.time;
        }
    }
    
	
    public void walk(float intensity)
    {
        if (!walkedThisFrame)
        {
            bool oppositeDirectionOfMovement = body.velocity.x * intensity < 0;

            // calculate the force of our movement, cancelling out force of friction if we're running in the same direction as we're moving
            // getGroundNormal().Rotate(-90f) will return the direction of the ground we're walking on. But we need to make sure we're on the ground
            var walkDirection = (isOnGround() ? getGroundNormal().Rotate(-90f) : Vector2.right) * intensity;
            var f = walkDirection * (oppositeDirectionOfMovement ? (counterForce) : (runAccel)); // 

            applyContinuousForce(f, maxRunVel);
            walkedThisFrame = true;


        }
    }
    

    public void shootGrapple(Vector3 position)
    {
        if (grappleObject) Destroy(grappleObject);

        grappleObject = new GameObject("grappleHost");
        grappleObject.transform.parent = transform;
        grappleObject.transform.position = Vector3.zero;

        grapple = grappleObject.AddComponent<Grapple>();
        grapple.shoot(this, position - transform.position);
    }

    public void releaseGrapple()
    {
        if (grappleObject) Destroy(grappleObject);

    }

    public bool isOnGround()
    {
        return raycastDown(raycastDownDist).Any(r => Vector2.Angle(Vector2.up, r.normal) < 90); // make sure that we didn't collide with a wall
    }

    public Vector2 getGroundNormal() // assumes character is grounded
    {
        var hits = raycastDown(raycastDownDist).Where(x => x);
        if (body.velocity.x < 0) // if we're moving left, return the first (and therefore leftmost) raycast's normal
            return hits.First().normal;
        else
            return hits.Last().normal; 
    }

    public float forceofFriction()
    {
        var frictionCoefficient = isOnGround() ? groundFriction : airFriction;
        // Time.fixedDeltaTime is the time since the last physics tick
        // every physics tick, we want to reduce the character's x velocity based on it's coeeficient of friction
        // the faster it's going the more we want to reduce it
        // we could just write ``velocity.x -= velocity.x * .9f * Time.FixedDeltaTime`` to reduce characters speed 90% every second
        // This has different behavior at different physics tick rates, but I'm okay with that
        return body.velocity.x * frictionCoefficient * Time.fixedDeltaTime;  
    }


    private void FixedUpdate()
    {
        if (!walkedThisFrame)
        {
            // calculate friction
            var oldVel = body.velocity;
            body.velocity -= new Vector2(forceofFriction(), 0);
            // Stop friction from making us switch directions (which clearly does not happen in real life)
            if (oldVel.x * body.velocity.x < 0) body.velocity = new Vector2(0, body.velocity.y);
        }
        walkedThisFrame = false;

        // if we're either going down or not holding the up button, increase gravity
        if (!grappleObject)
            body.gravityScale = (body.velocity.y < 0 || !tryingToGoUp) ? downwardGravityScale : upwardGravityScale;
        tryingToGoUp = false;
        if (isOnGround() && !jumpedThisFrame && (Time.time - timeSinceLastJump) > minSecondsBetweenTrumps) jumpsRemaining = numberOfJumps;

        body.velocity = new Vector2(Mathf.Clamp(body.velocity.x, -maxXVel, maxXVel), Mathf.Clamp(body.velocity.y, -maxYVel, maxYVel));

    }

    void Update () {
        jumpedThisFrame = false;
    }

    public void LateUpdate()
    {
        jumpedThisFrame = false;
    }



    public float Sign(float f)
    {
        if (f < 0)
            return -1f;
        if (f > 0)
            return 1f;
        return 0f;
    }
}
