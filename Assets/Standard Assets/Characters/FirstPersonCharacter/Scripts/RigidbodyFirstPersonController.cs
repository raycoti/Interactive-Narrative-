using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof (Rigidbody))]
    [RequireComponent(typeof (CapsuleCollider))]
    public class RigidbodyFirstPersonController : MonoBehaviour
    {
        [Serializable]
        public class MovementSettings
        {
            public float ForwardSpeed = 8.0f;   // Speed when walking forward
            public float BackwardSpeed = 4.0f;  // Speed when walking backwards
            public float StrafeSpeed = 4.0f;    // Speed when walking sideways
            public float RunMultiplier = 2.0f;   // Speed when sprinting
	        public KeyCode RunKey = KeyCode.LeftShift;
            public float JumpForce = 30f;
            public AnimationCurve SlopeCurveModifier = new AnimationCurve(new Keyframe(-90.0f, 1.0f), new Keyframe(0.0f, 1.0f), new Keyframe(90.0f, 0.0f));
            [HideInInspector] public float CurrentTargetSpeed = 8f;

#if !MOBILE_INPUT
            private bool m_Running;
#endif

            public void UpdateDesiredTargetSpeed(Vector2 input)
            {
	            if (input == Vector2.zero) return;
				if (input.x > 0 || input.x < 0)
				{
					//strafe
					CurrentTargetSpeed = StrafeSpeed;
				}
				if (input.y < 0)
				{
					//backwards
					CurrentTargetSpeed = BackwardSpeed;
				}
				if (input.y > 0)
				{
					//forwards
					//handled last as if strafing and moving forward at the same time forwards speed should take precedence
					CurrentTargetSpeed = ForwardSpeed;
				}
#if !MOBILE_INPUT
	            if (Input.GetKey(RunKey))
	            {
		            CurrentTargetSpeed *= RunMultiplier;
		            m_Running = true;
	            }
	            else
	            {
		            m_Running = false;
	            }
#endif
            }

#if !MOBILE_INPUT
            public bool Running
            {
                get { return m_Running; }
            }
#endif
        }


        [Serializable]
        public class AdvancedSettings
        {
            public float groundCheckDistance = 0.01f; // distance for checking if the controller is grounded ( 0.01f seems to work best for this )
            public float stickToGroundHelperDistance = 0.5f; // stops the character
            public float slowDownRate = 20f; // rate at which the controller comes to a stop when there is no input
            public bool airControl; // can the user control the direction that is being moved in the air
        }


        // Flying ignores gravity and passes through everything except invisible walls.
        [Serializable]
        public class FlySettings
        {
            public bool Allowed = true;            // whether players can switch to flying
            public KeyCode ToggleKey = KeyCode.F;
            public KeyCode UpKey = KeyCode.E;      // Jump (Space) also rises
            public KeyCode DownKey = KeyCode.Q;
            public float Speed = 15f;              // the run key multiplies this by RunMultiplier
            public float ClimbSpeed = 10f;
        }


        // Objects on this layer (see InvisibleWall) stop a flying player and nothing else.
        public const string InvisibleWallLayer = "InvisibleWall";

        public Camera cam;
        public MovementSettings movementSettings = new MovementSettings();
        public MouseLook mouseLook = new MouseLook();
        public AdvancedSettings advancedSettings = new AdvancedSettings();
        public FlySettings flySettings = new FlySettings();


        private Rigidbody m_RigidBody;
        private CapsuleCollider m_Capsule;
        private float m_YRotation;
        private Vector3 m_GroundContactNormal;
        private bool m_Jump, m_PreviouslyGrounded, m_Jumping, m_IsGrounded;
        private bool m_Held, m_WaitForRelease;
        private bool m_IgnoreBack;           // after turning around, back is ignored until let go
        private float m_GlideTime;           // time left in a glide; input is ignored meanwhile
        private bool m_GlideTurns;           // whether the glide also turns the view to m_LookAt
        private Vector3 m_GlideTo, m_LookAt, m_LeaveTo;
        private const float GlideDuration = 0.6f;
        private bool m_Flying;
        private Vector3 m_FlyVelocity;
        private int m_WallMask;              // the invisible wall layer
        private const float WallSkin = 0.05f; // gap kept between a flying player and a wall


        public Vector3 Velocity
        {
            get { return m_RigidBody.linearVelocity; }
        }

        public bool Grounded
        {
            get { return m_IsGrounded; }
        }

        public bool Jumping
        {
            get { return m_Jumping; }
        }

        // True while stopped by HoldUntilWalk.
        public bool Held
        {
            get { return m_Held; }
        }

        public bool Flying
        {
            get { return m_Flying; }
        }

        // Rise (> 0) or sink (< 0) while flying, from -1 to 1; set by the touch controls.
        public float TouchClimb { get; set; }

        public bool Running
        {
            get
            {
 #if !MOBILE_INPUT
				return movementSettings.Running;
#else
	            return false;
#endif
            }
        }


        // Stops the player, glides them to standAt (keeping their height) while turning to
        // face lookAt, then holds them there. Pressing forward lets them walk on; back glides
        // them to leaveTo and turns them around; left or right turns them to face that way.
        // Forward is usually held when this is called, so it has to be let go first. Looking
        // around still works once the glide ends.
        public void HoldUntilWalk(Vector3 standAt, Vector3 lookAt, Vector3 leaveTo)
        {
            m_Held = true;
            m_WaitForRelease = true;
            m_Jump = false;
            if (m_Flying)
                m_FlyVelocity = Vector3.zero;
            else
                m_RigidBody.linearVelocity = new Vector3(0f, m_RigidBody.linearVelocity.y, 0f);
            m_LookAt = lookAt;
            m_LeaveTo = leaveTo;
            Glide(standAt, true);
        }


        // Ends a hold the way a walk key does: walk > 0 walks on, walk < 0 glides back out and
        // turns round, otherwise side turns to face left (< 0) or right (> 0). Walk keys and the
        // touch buttons both come through here.
        public void ReleaseHold(float walk, float side)
        {
            if (!m_Held) return;
            m_Held = false;
            if (walk > 0f)
            {
                m_GlideTime = 0f;                  // walk on from here
            }
            else if (walk < 0f)
            {
                // Back straight out, clear of the text, turning round a random way to
                // face the way out. 179 rather than 180 degrees fixes which way it swings.
                // Back now points at the text again, so it's ignored until let go.
                FaceTurned(UnityEngine.Random.value < 0.5f ? 179f : -179f);
                m_IgnoreBack = true;
                Glide(m_LeaveTo, true);
            }
            else
            {
                // Turn in place to face left or right.
                FaceTurned(side < 0f ? -90f : 90f);
                Glide(m_RigidBody.position, true);
            }
        }


        // Points the next glide's view turn at the horizon, the given angle round from where the
        // player faces now (positive is to the right).
        private void FaceTurned(float degrees)
        {
            var facing = transform.forward;
            facing.y = 0f;
            m_LookAt = cam.transform.position + Quaternion.AngleAxis(degrees, Vector3.up)*facing.normalized*1000f;
        }


        private void Glide(Vector3 to, bool turnView)
        {
            m_GlideTo = to;
            m_GlideTurns = turnView;
            m_GlideTime = GlideDuration;
        }


        // Switches between walking and flying. Flying makes the body kinematic, so it ignores
        // gravity and every collider, and FlyMove sweeps it against invisible walls itself.
        // Triggers such as the poem text boxes still fire.
        public void SetFlying(bool fly)
        {
            if (fly == m_Flying || (fly && !flySettings.Allowed)) return;
            m_Flying = fly;
            m_Jump = false;
            m_Jumping = false;
            if (fly)
            {
                m_FlyVelocity = m_RigidBody.linearVelocity;
                m_RigidBody.isKinematic = true;
            }
            else
            {
                // Carry the flight's momentum into the fall.
                m_RigidBody.isKinematic = false;
                m_RigidBody.linearVelocity = m_FlyVelocity;
                m_FlyVelocity = Vector3.zero;
            }
        }


        private void Start()
        {
            m_RigidBody = GetComponent<Rigidbody>();
            m_Capsule = GetComponent<CapsuleCollider>();
            mouseLook.Init (transform, cam.transform);
            m_WallMask = LayerMask.GetMask(InvisibleWallLayer);
            // Walking passes through invisible walls; only flying is stopped by them.
            m_RigidBody.excludeLayers = m_WallMask;
        }


        private void Update()
        {
            if (m_GlideTime > 0f)
                m_GlideTime -= Time.deltaTime;
            if (m_GlideTime > 0f && m_GlideTurns)
                mouseLook.TurnTowards(transform, cam.transform, m_LookAt, 1f - Mathf.Exp(-8f*Time.deltaTime));
            else
                RotateView();

            if (flySettings.Allowed && Input.GetKeyDown(flySettings.ToggleKey))
            {
                SetFlying(!m_Flying);
            }

            if (CrossPlatformInputManager.GetButtonDown("Jump") && !m_Jump && !m_Held && !m_Flying)
            {
                m_Jump = true;
            }
        }


        private void FixedUpdate()
        {
            if (m_Flying)
            {
                FlyMove();
                return;
            }

            GroundCheck();
            Vector2 input = GetInput();

            var gliding = m_GlideTime > 0f;
            if (gliding)
            {
                // Steer by velocity so the glide collides with walls, easing in towards the target.
                var step = (m_GlideTo - m_RigidBody.position)*(1f - Mathf.Exp(-8f*Time.fixedDeltaTime))/Time.fixedDeltaTime;
                m_RigidBody.WakeUp();
                m_RigidBody.linearVelocity = new Vector3(step.x, m_RigidBody.linearVelocity.y, step.z);
            }

            if ((Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon) && (advancedSettings.airControl || m_IsGrounded))
            {
                // always move along the camera forward as it is the direction that it being aimed at
                Vector3 desiredMove = cam.transform.forward*input.y + cam.transform.right*input.x;
                desiredMove = Vector3.ProjectOnPlane(desiredMove, m_GroundContactNormal).normalized;

                desiredMove.x = desiredMove.x*movementSettings.CurrentTargetSpeed;
                desiredMove.z = desiredMove.z*movementSettings.CurrentTargetSpeed;
                desiredMove.y = desiredMove.y*movementSettings.CurrentTargetSpeed;
                if (m_RigidBody.linearVelocity.sqrMagnitude <
                    (movementSettings.CurrentTargetSpeed*movementSettings.CurrentTargetSpeed))
                {
                    m_RigidBody.AddForce(desiredMove*SlopeMultiplier(), ForceMode.Impulse);
                }
            }

            if (m_IsGrounded)
            {
                // No drag mid-glide, or it would cut the glide short.
                m_RigidBody.linearDamping = gliding ? 0f : 5f;

                if (m_Jump)
                {
                    m_RigidBody.linearDamping = 0f;
                    m_RigidBody.linearVelocity = new Vector3(m_RigidBody.linearVelocity.x, 0f, m_RigidBody.linearVelocity.z);
                    m_RigidBody.AddForce(new Vector3(0f, movementSettings.JumpForce, 0f), ForceMode.Impulse);
                    m_Jumping = true;
                }

                // Input is zeroed mid-glide, so sleeping here would cancel the glide.
                if (!gliding && !m_Jumping && Mathf.Abs(input.x) < float.Epsilon && Mathf.Abs(input.y) < float.Epsilon && m_RigidBody.linearVelocity.magnitude < 1f)
                {
                    m_RigidBody.Sleep();
                }
            }
            else
            {
                m_RigidBody.linearDamping = 0f;
                if (m_PreviouslyGrounded && !m_Jumping)
                {
                    StickToGroundHelper();
                }
            }
            m_Jump = false;
        }


        private void FlyMove()
        {
            Vector2 input = GetInput();
            Vector3 desired;
            if (m_GlideTime > 0f)
            {
                // Glide across to the poem text box at the current height.
                var step = (m_GlideTo - m_RigidBody.position)*(1f - Mathf.Exp(-8f*Time.fixedDeltaTime))/Time.fixedDeltaTime;
                desired = m_FlyVelocity = new Vector3(step.x, 0f, step.z);
            }
            else
            {
                // Fly where the camera points, so looking up and moving forward climbs.
                var move = Vector3.ClampMagnitude(cam.transform.forward*input.y + cam.transform.right*input.x, 1f);
                var speed = flySettings.Speed;
#if !MOBILE_INPUT
                if (Input.GetKey(movementSettings.RunKey)) speed *= movementSettings.RunMultiplier;
#endif
                var climb = TouchClimb;
                if (Input.GetKey(flySettings.UpKey) || CrossPlatformInputManager.GetButton("Jump")) climb += 1f;
                if (Input.GetKey(flySettings.DownKey)) climb -= 1f;
                if (m_Held) climb = 0f;
                desired = move*speed + Vector3.up*Mathf.Clamp(climb, -1f, 1f)*flySettings.ClimbSpeed;
                // Ease in and out, as the walking drag does.
                m_FlyVelocity = Vector3.Lerp(m_FlyVelocity, desired, 1f - Mathf.Exp(-6f*Time.fixedDeltaTime));
            }
            m_RigidBody.MovePosition(SweepAgainstWalls(m_RigidBody.position, m_FlyVelocity*Time.fixedDeltaTime));
            m_Jump = false;
        }


        // Moves the capsule from position by motion, sliding along any invisible wall in the way.
        private Vector3 SweepAgainstWalls(Vector3 position, Vector3 motion)
        {
            var scale = transform.lossyScale;
            var radius = m_Capsule.radius*Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            var halfLine = Mathf.Max(0f, m_Capsule.height*Mathf.Abs(scale.y)*0.5f - radius);
            var offset = transform.rotation*Vector3.Scale(m_Capsule.center, scale);
            for (int i = 0; i < 3 && motion.sqrMagnitude > 1e-8f; i++)
            {
                var centre = position + offset;
                var distance = motion.magnitude;
                var direction = motion/distance;
                RaycastHit hit;
                // A wall the capsule already overlaps reports a zero point; let the player out of it.
                if (!Physics.CapsuleCast(centre + Vector3.up*halfLine, centre - Vector3.up*halfLine, radius, direction,
                                         out hit, distance + WallSkin, m_WallMask, QueryTriggerInteraction.Ignore)
                    || (hit.distance <= 0f && hit.point == Vector3.zero))
                {
                    return position + motion;
                }
                var travel = Mathf.Max(0f, hit.distance - WallSkin);
                position += direction*travel;
                motion = Vector3.ProjectOnPlane(direction*(distance - travel), hit.normal);
                // Stop pushing into the wall so the flight doesn't keep sliding once let go.
                m_FlyVelocity = Vector3.ProjectOnPlane(m_FlyVelocity, hit.normal);
            }
            return position;
        }


        // What a walking player can stand on: the usual raycast layers minus invisible walls.
        private int WalkableLayers
        {
            get { return Physics.DefaultRaycastLayers & ~m_WallMask; }
        }


        private float SlopeMultiplier()
        {
            float angle = Vector3.Angle(m_GroundContactNormal, Vector3.up);
            return movementSettings.SlopeCurveModifier.Evaluate(angle);
        }


        private void StickToGroundHelper()
        {
            RaycastHit hitInfo;
            if (Physics.SphereCast(transform.position, m_Capsule.radius, Vector3.down, out hitInfo,
                                   ((m_Capsule.height/2f) - m_Capsule.radius) +
                                   advancedSettings.stickToGroundHelperDistance, WalkableLayers))
            {
                if (Mathf.Abs(Vector3.Angle(hitInfo.normal, Vector3.up)) < 85f)
                {
                    m_RigidBody.linearVelocity = Vector3.ProjectOnPlane(m_RigidBody.linearVelocity, hitInfo.normal);
                }
            }
        }


        private Vector2 GetInput()
        {
            
            Vector2 input = new Vector2
                {
                    x = CrossPlatformInputManager.GetAxis("Horizontal"),
                    y = CrossPlatformInputManager.GetAxis("Vertical")
                };
            if (m_Held)
            {
                // Raw input, so letting go registers at once rather than after the axis eases out.
                var walk = CrossPlatformInputManager.GetAxisRaw("Vertical");
                var side = CrossPlatformInputManager.GetAxisRaw("Horizontal");
                if (walk == 0f && side == 0f) m_WaitForRelease = false;
                else if (!m_WaitForRelease) ReleaseHold(walk, side);
            }
            if (m_IgnoreBack && CrossPlatformInputManager.GetAxisRaw("Vertical") >= 0f)
                m_IgnoreBack = false;
            if (m_Held || m_GlideTime > 0f || m_IgnoreBack) input = Vector2.zero;
			movementSettings.UpdateDesiredTargetSpeed(input);
            return input;
        }


        private void RotateView()
        {
            //avoids the mouse looking if the game is effectively paused
            if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

            // get the rotation before it's changed
            float oldYRotation = transform.eulerAngles.y;

            mouseLook.LookRotation (transform, cam.transform);

            // A flying body is kinematic, and FlyMove already moves where the camera points.
            if (!m_Flying && (m_IsGrounded || advancedSettings.airControl))
            {
                // Rotate the rigidbody velocity to match the new direction that the character is looking
                Quaternion velRotation = Quaternion.AngleAxis(transform.eulerAngles.y - oldYRotation, Vector3.up);
                m_RigidBody.linearVelocity = velRotation*m_RigidBody.linearVelocity;
            }
        }


        /// sphere cast down just beyond the bottom of the capsule to see if the capsule is colliding round the bottom
        private void GroundCheck()
        {
            m_PreviouslyGrounded = m_IsGrounded;
            RaycastHit hitInfo;
            if (Physics.SphereCast(transform.position, m_Capsule.radius, Vector3.down, out hitInfo,
                                   ((m_Capsule.height/2f) - m_Capsule.radius) + advancedSettings.groundCheckDistance, WalkableLayers))
            {
                m_IsGrounded = true;
                m_GroundContactNormal = hitInfo.normal;
            }
            else
            {
                m_IsGrounded = false;
                m_GroundContactNormal = Vector3.up;
            }
            if (!m_PreviouslyGrounded && m_IsGrounded && m_Jumping)
            {
                m_Jumping = false;
            }
        }
    }
}
