using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;




[RequireComponent(typeof(Rigidbody2D))]
public class CharacterControllerPlatformer : MonoBehaviour {
    Rigidbody2D body;
    BoxCollider2D boxCol;
    public LayerMask groundLayer;

    public float maxRunVel = 3;
    public float runAccel = 5;     // acceleration when running 
    public float counterForce = 8; // acceleration when trying to move the opposite direction as the movement
    public float groundFriction = .9f;

    public float jumpVelocity = 2;
    public int numberOfJumps = 2;
    int jumpsRemaining = 0;
    public float upwardGravityScale = .9f;
    public float downwardGravityScale = 1.3f;
    public float airFriction = .3f;

    public float maxXVel = 20;
    public float maxYVel = 50;

    bool jumpedThisFrame = false;
    bool walkedThisFrame = false;
    bool tryingToGoUp = false;
    float timeSinceLastJump;
    float minSecondsBetweenTrumps = .2f;

    float raycastDownDist = .1f;



	void Start ()
    {
        body = GetComponent<Rigidbody2D>();
        boxCol = GetComponent<BoxCollider2D>();

        timeSinceLastJump = Time.time;
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
            Debug.Log(body.velocity);
            bool oppositeDirectionOfMovement = Mathf.Sign(body.velocity.x * intensity) < 0;
            bool lessThanMaxVelocity = Mathf.Abs(body.velocity.x) <= maxRunVel;
            if (lessThanMaxVelocity || oppositeDirectionOfMovement)
            {
                // calculate the force of our movement, cancelling out force of friction if we're running in the same direction as we're moving
                var f = Vector2.right * intensity * (oppositeDirectionOfMovement ? (counterForce) : (runAccel));

                if (!lessThanMaxVelocity)
                    Physics2DExtensions.AddForce(body, f, ForceMode.VelocityChange);
                else if (Mathf.Abs(body.velocity.x + f.x) <= maxRunVel) //if the speed addition wouldn't make us accelerate past the max run speed
                    Physics2DExtensions.AddForce(body, f, ForceMode.VelocityChange);
                else
                    body.velocity = new Vector2(Sign(body.velocity.x) * maxRunVel, body.velocity.y);
            }

            walkedThisFrame = true;
        }
    }

    public bool isOnGround()
    {
        return raycastDown(raycastDownDist).Any(r => r);
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
            // Stop friction from making us switch directions (which does not happen in real life)
            if (oldVel.x * body.velocity.x < 0) body.velocity = new Vector2(0, body.velocity.y);
        }
        walkedThisFrame = false;

        // if we're either going down or not holding the up button, increase gravity
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
