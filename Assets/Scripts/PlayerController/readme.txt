
Starter kit:
boxcollider2d
rigidbody2d
player controller platformer
character controller platformer
a physic2d material on the rigid body with no friction



help
my character can jump too many times
this may have something to do with your script execution order. Ideally character controller platformer would run after whatever's making it jump in the script execution order.


todo: 
conserve all velocity when moving on or off a slope
✓ grapple