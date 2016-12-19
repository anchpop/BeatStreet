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
    public float runAccel = 5;
    public float jumpForce = 2;


	void Start ()
    {
        body = GetComponent<Rigidbody2D>();
        boxCol = GetComponent<BoxCollider2D>();

    }

    /*RaycastHit2D raycastDown()
    {
    }*/

    public void jump()
    {
        Physics2DExtensions.AddForce(body, Vector2.up * jumpForce, ForceMode.Impulse);

    }
	
    public void walk(float intensity)
    {
        Debug.Log(body.velocity);
        if (Mathf.Abs(body.velocity.x) < maxRunVel)
        {
            Physics2DExtensions.AddForce(body, Vector2.right * intensity * runAccel, ForceMode.Acceleration);

        }
    }

	void Update () {
	
	}
}
