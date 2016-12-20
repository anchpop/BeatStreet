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
    public float jumpForce = 2;
    
    bool jumpedThisFrame = false;

    float raycastDownDist = .1f;


	void Start ()
    {
        body = GetComponent<Rigidbody2D>();
        boxCol = GetComponent<BoxCollider2D>();

    }

    List<RaycastHit2D> raycastDown()
    {
        var bottomRight = boxCol.bounds.center + new Vector3(boxCol.bounds.extents.x, -boxCol.bounds.extents.y, 0);
        var bottomLeft  = boxCol.bounds.center + new Vector3(-boxCol.bounds.extents.x, -boxCol.bounds.extents.y, 0);
        List<RaycastHit2D> hits = new List<RaycastHit2D>();
        hits.Add(Physics2D.Raycast(bottomLeft, Vector3.down, raycastDownDist, groundLayer));
        hits.Add(Physics2D.Raycast(bottomRight, Vector3.down, raycastDownDist, groundLayer));
        return hits;
    }

    public void jump()
    {
        if (isOnGround() && !jumpedThisFrame)
        {
            Physics2DExtensions.AddForce(body, Vector2.up * jumpForce, ForceMode.Impulse);
            jumpedThisFrame = true;
        }

    }
	
    public void walk(float intensity)
    {
        Debug.Log(body.velocity);
        bool oppositeDirectionOfMovement = Mathf.Sign(body.velocity.x * intensity) < 0;
        if (Mathf.Abs(body.velocity.x) < maxRunVel || oppositeDirectionOfMovement) 
        {
            var f = Vector2.right * intensity * (oppositeDirectionOfMovement ? counterForce : runAccel); 
            Physics2DExtensions.AddForce(body, f, ForceMode.Acceleration);

        }
    }

    public bool isOnGround()
    {
        return raycastDown().Any(r => r);
    }

	void Update () {
        jumpedThisFrame = false;
    }
}
