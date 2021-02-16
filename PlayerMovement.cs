using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public enum PlayerStates
    {
        Static, //default state
        Grounded,//on the ground
        InAir, //in the air
        Held, //we are being held by another unit
        WaterSwimming, //we are swimming in the water
    }

    //scripts and controllers
    private PlayerCollision Coli;
    private Rigidbody Rigid;
    private Animator Anim;
    private CameraCtrl Cam;
    private PlayerVisual Visual;
    private GamePlayer GameCtrl;

    [Header("Override")]
    public bool FollowForwardDir; //if we move by our forward rotation 
    public bool BounceOnGround; //if we bounce when hitting the floor
    public bool NoRotation; //if the player does not rotate and rolls instead
    public bool JumpBasedOnTransform; //if our jumping is based on our transform rotation
    public bool RotateToGroundInAir; //if we rotate towards the ground when in mid air
    public bool CannotJump; //we cannot jump
    public bool CannotSwim; //we cannot swim

    [Header("Physics")]
    public float MaxSpeed; //how fast we run forward
    public float MinSpeed; //the least amount of speed
    public float SpeedClamp;//how fast we can possibly be
    [Range(0, 1)]
    public float InAirControl; //how much control you have over your movement direction when in air
    public float ActSpeed; //how much speed is applied to the rigidbody
    public float Acceleration; //how fast we build speed
    public float Decceleration; //how fast we slow down
    public float DirectionControl = 8; //how much control we have over changing direction
    private float AdjustMentCtrl = 1; //a lerp for how much control we have over our character
    private PlayerStates CurrentState; //the current state the player is in
    private float InAirTimer; //how long we are in the air for (this is for use when wall running or falling off the ground
    private float OnGroundTimer; //how long we are on the ground for
    private float WaterTimer;
    private float CurrentMag = 0; //our current input magnitude

    [Header("Water")]
    public float WaterSpeed; //our running speed underwater
    public float WaterGravity; //waters gravity
    public float WaterAdjustment; //how quickly we adjust to waters gravity
    public float SwimHeight; //how much height we get per swim    
    public float SwimForward; //how much we swim forward on a jump
    public float WaterExitHeight; //how high we exit the water
    public float TimeBtwSwimJump = 0.15f; //how frequently we can jump underwater
    public float WaterControl; //how much control of movement we have in water

    [Header("SlopeMovement")]
    public float MaxSlopeAmt; //the max  slope that will influence our player
    public float MinSlopeAmt; //the smallest slope that will influence our player
    public float SlopeSpeedGain; //the max speed we can add to our controller
    public float SlopeSpeedLoss; //the min speed we can add to our controller
    public float Stickyness; //how much we stick to slopes

    [Header("DistanceCheck")]
    public float SpeedNeededToCheck; //how fast we must be going to check distance
    public float TimeBtwSpeedChecks; //how frequently we check for distance between positions
    private float SpeedCheckTime; //actual timer
    private Vector3 LastPosition; //the last position we were at
    public float DistanceNeeded; //the distance needed for are player to move

    [Header("Turning")]
    public float TurnSpeed; //how fast we turn when on the ground
    public float TurnSpeedInAir; //how fast we turn when in air
    public float TurnSpeedOnWalls; //how fast we are when running on a wall
    public float TurnSpeedInWater; //how fast we turn underwater


    [Header("Jumping")]
    public float JumpHeight; //how high we jump
    [Range(0, 10)]
    public float JumpForwardMod;

    [Header("Better Jumping")]
    public float JumpHoldAmt; //how much holding jump adds to a jump 
    public float JumpHoldTime; //how long we can hold jump for
    public float fallMultiplier = 2.5f; //multiplier to falling 
    public float lowJumpMultiplier = 2f; //multiplier to begining of jump

    [Header("ObjectGrabbing")]
    public Transform BaseTransform; //the position we base our grabbed position on
    public bool CanGrab; //if we can grab objects
    public bool CanBeGrabbed; //if we can be grabbed
    public float ThrowForce; //how much we throw objects upwards
    public float ThrowForceForwards; //how much we throw objects forwards
    public float DropForce = 3f; //how much we drop objects upwards
    public float DropForceForwards = 2.5f; //how much we drop objects forwards
    private float MovAdjustment;
    private GameObject HeldObj; //what we are holding
    private PlayerMovement HoldingUsObj; //what is holding us
    private Collider OurCollider; //the collider for our object 
    private CapsuleCollider GrabbingCollider; //the collider to adjust to any additional objects we are grabbing


    // Start is called before the first frame update
    public void Setup(CameraCtrl Cm, GamePlayer Ply)
    {
        //get all info
        Coli = GetComponent<PlayerCollision>();
        Rigid = GetComponent<Rigidbody>();
        Anim = GetComponentInChildren<Animator>();
        Visual = GetComponent<PlayerVisual>();
        //get our colliders
        OurCollider = GetComponent<Collider>();
        GrabbingCollider = GetComponentInChildren<CapsuleCollider>();

        Cam = Cm;
        GameCtrl = Ply;

        //set animation offset
        if (Anim)
        {
            Anim.SetFloat("Offset", Random.Range(0.85f, 1.15f));
            Anim.SetFloat("Offset2", Random.Range(0f, 1f));
        }

        //set into ground state
        SetOnGround();
    }


    void AnimCtrl()
    {
        if (!Anim)
            return;

       //current animated state
        int State = 0;
        if (CurrentState == PlayerStates.InAir)
            State = 1;
        else if (CurrentState == PlayerStates.WaterSwimming)
            State = 5;
        else if (CurrentState == PlayerStates.Held)
            State = 6;

        //set animation states
        Anim.SetInteger("State", State);
        //Anim.SetBool("Crouching", Crouch);
        //set velocity states
        Vector3 Vel = transform.InverseTransformDirection(Rigid.velocity);
        Anim.SetFloat("XVelocity", Vel.x);
        Anim.SetFloat("ZVelocity", Vel.z);
        Anim.SetFloat("YVelocity", Rigid.velocity.y);
        Anim.SetFloat("Speed", ActSpeed);
    }

    public void Tick(float Del, float horInput, float verInput)
    {
        //check animations
        AnimCtrl();

        //get magnituded of our inputs
        float InputMagnitude = new Vector2(horInput, verInput).normalized.magnitude;
        CurrentMag = InputMagnitude; //set our current magnitude
        //movement direction
        Debug.Log("need movement direction relative to transform up");
        Vector3 MoveDir = (Cam.transform.forward * verInput) + (Cam.transform.right * horInput);
        MoveDir.y = 0;

        //handle our fov
        //HandleFov(Del);

        if (CurrentState == PlayerStates.Grounded)
        {
            //tick our ground timer
            if (OnGroundTimer < 10)
                OnGroundTimer += Del;

            //check for water
            bool Water = Coli.CheckWater();
            if (Water)
            {
                //we hit water
                SetUnderwater();
                return;
            }

            //check for the ground 
            Vector3 Grounded = Coli.CheckFloor(-Vector3.up);

            //we are in the air
            if (Grounded == Vector3.zero)
            {
                if (InAirTimer > 0.05f)
                {
                    SetInAir();
                    return;
                }
                else
                    InAirTimer += Del;
            }
            else
                InAirTimer = 0;

            //get the amount of speed, based on if we press forwards or backwards
            float TargetSpd = MaxSpeed; //get our speed
            float SlopeAmt = 0; //get slope amount to apply to speed
            float GroundAngle = Vector3.Angle(Grounded, Vector3.up); //get slope angle 
            Vector3 Sticky = Vector3.zero; //set the sticky amount

            //if we are crouching our target speed is our crouch speed
            //if (Crouch)
            //  TargetSpd = CrouchSpeed;

            //if our ground angle is more than our slope limit, slide off the surface
            if (GroundAngle > SlideSlopeAmt)
            {
                //if we are running up or standing on a slope slide down it
                if (Rigid.velocity.y >= 0)
                {

                    if (OnGroundTimer > 0.05f)
                    {

                        SlideSelf();
                        return;
                    }
                }
            }

            //set our sticky
            if(GroundAngle > MinSlopeAmt || GroundAngle < -MinSlopeAmt)
                Sticky = Grounded * Stickyness;

            //we are running up a hill
            if (Rigid.velocity.y > MinSlopeAmt)
            {
                if(ActSpeed > 7)
                {
                    float LossAmt = Rigid.velocity.y / MaxSlopeAmt;
                    float SpeedLoss = Mathf.Lerp(0, SlopeSpeedLoss, LossAmt);
                    SlopeAmt = -SpeedLoss;
                }
            }
            else if (Rigid.velocity.y < -MinSlopeAmt) //we are running down a hill, with a boost
            {
                float GainAmt = -Rigid.velocity.y / -MaxSlopeAmt;
                GainAmt = GainAmt * -1;
                float SpeedGain = Mathf.Lerp(0, SlopeSpeedGain, GainAmt);
                SlopeAmt = SpeedGain;
            }

            //check we are not moving against a walls
            if (ActSpeed >= SpeedNeededToCheck)
            {
                //get distance
                float DistanceCheck = CheckDis(Del);
                //multiply the speed by distance
                TargetSpd = TargetSpd * DistanceCheck;
            }

            //accelerate speed
            LerpSpeed(InputMagnitude, Del, TargetSpd, SlopeAmt);
            //lerp our adjustment control
            float Ctrl = DirectionControl;
            if(AdjustMentCtrl < 1)
            {
                Ctrl = Ctrl * AdjustMentCtrl;
                AdjustMentCtrl += Del;
            }

            //move and turn player
            MovePlayer(MoveDir, InputMagnitude, Del, Ctrl, Sticky);
            TurnPlayer(MoveDir, Del, TurnSpeed, Grounded);

            //check for crouching 
            /*
            if(Input.GetButton("Crouching"))
            { 
                //start crouching
                if(!Crouch)
                {
                    StartCrouch();
                }
            }
            else if(Crouch)
            {
                //stand up
                StopCrouching();
            } 
            */
        }
        else if(CurrentState == PlayerStates.InAir)
        {
            //tick our Air timer
            if (InAirTimer < 10)
                InAirTimer += Del;
            //if we can hold jump
            if(InAirTimer < JumpHoldTime)
            {
                //if jump is held
                if (Input.GetButton("Jump"))
                {
                    if(JumpBasedOnTransform)
                        Rigid.velocity += transform.up * JumpHoldAmt;
                    else
                        Rigid.velocity += Vector3.up * JumpHoldAmt;

                }
                  
            }

            //lerp speed
            float TargetSpd = MaxSpeed;
            float ModTime = Del * 0.5f;
            //check we are not moving against a walls
            if (ActSpeed >= SpeedNeededToCheck)
            {
                //get distance
                float DistanceCheck = CheckDis(Del);
                //multiply the speed by distance
                TargetSpd = TargetSpd * DistanceCheck;
            }

            //if we are not moving, and pressing an input. lerp our speed quickly
            if(InputMagnitude != 0)
            {
                if (ActSpeed < 7)
                {
                    //lerp quickly
                    ModTime = Del * 3;
                }
            }

            //Move our speed Slowly
            LerpSpeed(InputMagnitude, ModTime, TargetSpd, 0);

            //move player
            float Ctrl = DirectionControl * InAirControl;
            InAirMovement(MoveDir, InputMagnitude, Del, Ctrl);

            //if we rotate to ground while in air, find ground angle
            Vector3 FloorAng = Vector3.up;
            if (RotateToGroundInAir)
                FloorAng = Coli.FloorAngle();
            //turn our player with the in air modifier
            TurnPlayer(MoveDir, Del, TurnSpeedInAir, FloorAng);

            //check for the ground 
            if(InAirTimer > 0.25f)
            {
                Vector3 Grounded = Coli.CheckFloor(-Vector3.up);
                //we are on the ground (and have been in the air for a short time, to prevent multiple jump glitched
                if (Grounded != Vector3.zero)
                {
                    SetOnGround();
                    return;
                }

                //check for water
                bool Water = Coli.CheckWater();
                if (Water)
                {
                    //we hit water
                    SetUnderwater();
                    return;
                }
            }
        }      
        else if(CurrentState == PlayerStates.Held)
        {
            //if we are no longer held, set in air and return
            if (!HoldingUsObj)
            {
                SetInAir();
                return;
            }
            if (MovAdjustment < 1)
                MovAdjustment += Del * 2f;

            float Adjustment = 100 * MovAdjustment;

            //lerp our position to the holding us position + 1
            Vector3 HoldPos = HoldingUsObj.transform.position + (HoldingUsObj.transform.up * 1f);
            //calculate a held position based on the holding animation rig
            if (HoldingUsObj.BaseTransform)
            {
                Vector3 Pos = HoldingUsObj.BaseTransform.position;
                HoldPos.y = (Pos +(HoldingUsObj.BaseTransform.transform.up * 1f)).y;
            }
            Vector3 LerpPos = Vector3.Lerp(transform.position, HoldPos, Adjustment * Del);
            transform.position = LerpPos;
            //lerp our rotation to our our holders rotation
            Quaternion SlerpAmt = Quaternion.Slerp(transform.rotation, HoldingUsObj.transform.rotation, 16 * Del);
            transform.rotation = SlerpAmt;
        }
        else if(CurrentState == PlayerStates.WaterSwimming)
        {
            if (WaterTimer <= 5)
                WaterTimer += Del;
            //lerp speed
            float TargetSpd = WaterSpeed;
            LerpSpeed(InputMagnitude, Del, TargetSpd, 0);
            //move around in water (same system as on ground)
            float Ctrl = WaterControl;
            if (AdjustMentCtrl < 1)
            {
                Ctrl = Ctrl * AdjustMentCtrl;
                AdjustMentCtrl += Del;
            }
            //move and turn player
            WaterMovement(MoveDir, InputMagnitude, Del, Ctrl, WaterGravity);
            TurnPlayer(MoveDir, Del, TurnSpeedInWater, Vector3.up);

            bool Water = Coli.CheckWater();
            if (!Water)
            {
                //we are no longer underwater
                //check for the ground 
                Vector3 Grounded = Coli.CheckFloor(-Vector3.up);

                //we are in the air
                if (Grounded == Vector3.zero)
                {
                    //jump out of water
                    Visual.Splash();
                    SetInAir();
                    ExitWater();
                }
                else
                {
                    Visual.Splash();
                    SetOnGround();
                }
            }

        }
    }

    void SpeedBoost(float Amt)
    {
        //add to our speed
        ActSpeed += Amt;
        //stop crouching if we are
        //if(Crouch)
          //  StopCrouching();
    }

    public void BounceForce(float Amt, Vector3 Dir)
    {
        //add force
        Rigid.AddForce(Dir * Amt, ForceMode.Impulse); 
        //set our adjustment to 0
        AdjustMentCtrl = 0;
    }

    //lerp our current speed to our set max speed, by how much we are pressing the horizontal and vertical input
    void LerpSpeed(float InputMag, float D, float TargetSpeed, float SlopeBoost)
    {
        if (InputMag == 0 && ActSpeed == 0) //do not lerp on no speed
            return;
        else if (InputMag == 0 && ActSpeed < 0.1)
            ActSpeed = 0;

        //multiply our speed by our input amount
        float LerpAmt = TargetSpeed * InputMag;
        //get our acceleration (if we should speed up or slow down
        float Accel = Acceleration;
        if (InputMag == 0)
            Accel = Decceleration;

        //increase our actual speed by any slope boost
        if (SlopeBoost != 0)
            LerpAmt += SlopeBoost;

        //lerp by a factor of our acceleration
        ActSpeed = Mathf.Lerp(ActSpeed, LerpAmt, D * Accel);
        //add boost
        ActSpeed += SlopeBoost * D;
        //clamp speed
        ActSpeed = Mathf.Clamp(ActSpeed, MinSpeed, SpeedClamp);
    }

    //when in the air or on a wall, we set our action speed to the velocity magnitude, this is so that when we reach the ground again, our speed will carry over our momentum
    void SetSpeedToVelocity()
    {
        float Mag = new Vector2(Rigid.velocity.x, Rigid.velocity.z).magnitude;
        ActSpeed = Mag;
    }

    void SetInAir()
    {
       // if(Crouch)
         //   StopCrouching(); //cannot crouch in airosh

        //remove any extra downwards momentum
         //Vector3 VelAmt = new Vector3(Rigid.velocity.x, 0, Rigid.velocity.z);
         //Rigid.velocity = VelAmt;

        OnGroundTimer = 0; //remove the on ground timer
        CurrentState = PlayerStates.InAir;
    }

    void SetOnGround()
    {
        //remove any y velocity
        Vector3 VelAmt = new Vector3(Rigid.velocity.x, 0, Rigid.velocity.z);
        Rigid.velocity = VelAmt;

        //set our current speed to our velocity
        //SetSpeedToVelocity();

        InAirTimer = 0; //remove the in air timer
        CurrentState = PlayerStates.Grounded;
        //reset any flaps if we have them
        FlapsLeft = FlapAmt;

        //create landing fx 
        Visual.Landing();

        //check for bouncing
        if (BounceOnGround)
        {
            JumpUp();
            if (Anim)
                Anim.SetTrigger("Bounce");
            return;
        }
    }

    void SetUnderwater()
    {
        //remove thrown object
        if (HeldObj)
            ThrowObj(10f);

        //remove timer
        WaterTimer = 0;
        //create splash fx
        Visual.Splash();
        //we are swimming
        CurrentState = PlayerStates.WaterSwimming;
    }

    void TurnPlayer(Vector3 Dir, float D, float turn, Vector3 FloorDirection)
    {
        //we dont rotate
        if (NoRotation)
            return;

        //old rotation settings
        float singleStep = (turn * Time.deltaTime);
        /*
        //Rotate the forward vector towards the target direction by one step
        Vector3 newDirection = Vector3.RotateTowards(transform.forward, Dir, singleStep, 0.0f);

        //Calculate a rotation a step closer to the target and applies rotation to this object
        transform.rotation = Quaternion.LookRotation(newDirection);
        */
        //lerp our upwards rotation to stick to the floor
        Vector3 LerpDir = Vector3.Lerp(transform.up, FloorDirection, D * 8f);
        transform.rotation = Quaternion.FromToRotation(transform.up, LerpDir) * transform.rotation;

        //lerp our transform rotation to the direction of movement input
        if (Dir == Vector3.zero)
            Dir = transform.forward;
        Quaternion SlerpRot = Quaternion.LookRotation(Dir, transform.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, SlerpRot, turn * D);
    }
    
    void MovePlayer(Vector3 Dir, float InputAmt, float D, float Ctrl, Vector3 StickyGround) //direction to move, input of axis, delta, control over velocity
    {
        //find the direction to move in, based on the direction inputs
        Vector3 MovementDirection = Dir * InputAmt;
        MovementDirection = MovementDirection.normalized;
        //if we are no longer pressing and input, carryon moving in the last direction we were set to move in
        if (Dir == Vector3.zero || FollowForwardDir)
            MovementDirection = transform.forward;

        MovementDirection = MovementDirection * ActSpeed;

        //apply Gravity and Y velocity to the movement direction 
        MovementDirection.y = Rigid.velocity.y;

        float AdjustCtrl = Ctrl;
        //lerp to our movement direction based on how much airal control we have
        Vector3 LerpVelocity = Vector3.Lerp(Rigid.velocity, MovementDirection, AdjustCtrl * D);
        //stick to grounds
        LerpVelocity += -StickyGround;
        //set velocity
        Rigid.velocity = LerpVelocity;
    }

    void InAirMovement(Vector3 Dir, float InputAmt, float D, float Ctrl) //direction to move, input of axis, delta, control over velocity
    {
        //find the direction to move in, based on the direction inputs
        Vector3 MovementDirection = Dir * InputAmt;
        MovementDirection = MovementDirection.normalized;
        //if we are no longer pressing and input, carryon moving in the last direction we were set to move in
        if (Dir == Vector3.zero || FollowForwardDir)
            MovementDirection = transform.forward;

        MovementDirection = MovementDirection * ActSpeed;

        //apply Gravity and Y velocity to the movement direction 
        MovementDirection.y = Rigid.velocity.y;

        //better jumping
        if (MovementDirection.y < 0)
            MovementDirection.y += Physics.gravity.y * (fallMultiplier - 1) * D;
        else if (MovementDirection.y > 0)
            MovementDirection.y -= Physics.gravity.y * (lowJumpMultiplier - 1) * D;

        float AdjustCtrl = Ctrl;
        //lerp to our movement direction based on how much airal control we have
        Vector3 LerpVelocity = Vector3.Lerp(Rigid.velocity, MovementDirection, AdjustCtrl * D);
        //set velocity
        Rigid.velocity = LerpVelocity;
    }

    void SlidePlayer(float Slope, float Ctrl, float D)
    {
        //move ourself forawrds
        Vector3 MovementDirection = Rigid.velocity;
        //apply slope push
        MovementDirection -= Vector3.up * Slope;
        //lerp to our movement direction based on how much airal control we have
        Vector3 LerpVelocity = Vector3.Lerp(Rigid.velocity, MovementDirection, Ctrl * D);
        //apply some sticky
        LerpVelocity += -transform.up * 2f;
        Rigid.velocity = LerpVelocity;
    }

    void WaterMovement(Vector3 Dir, float InputAmt, float D, float Ctrl, float Gravity)
    {
        //find the direction to move in, based on the direction inputs
        Vector3 MovementDirection = Dir * InputAmt;
        MovementDirection = MovementDirection.normalized;
        //if we are no longer pressing and input, carryon moving in the last direction we were set to move in
        if (Dir == Vector3.zero || FollowForwardDir)
            MovementDirection = transform.forward;

        MovementDirection = MovementDirection * ActSpeed;

        //apply Gravity and Y velocity to the movement direction 
        MovementDirection.y = Mathf.Lerp(MovementDirection.y, -Gravity, WaterAdjustment * D);
        MovementDirection.y = Mathf.Clamp(Rigid.velocity.y, -Gravity, 1000);

        float AdjustCtrl = Ctrl;
        //lerp to our movement direction based on how much airal control we have
        Vector3 LerpVelocity = Vector3.Lerp(Rigid.velocity, MovementDirection, AdjustCtrl * D);
        //set velocity
        LerpVelocity.y = MovementDirection.y;
        Rigid.velocity = LerpVelocity;
    }

    public void JumpUp()
    {
        //if we are not on the ground of swimming
        if (CurrentState == PlayerStates.WaterSwimming)
        {
            //swim instead
            SwimUp();
            return;
        }
        //if we are in the air, check for flapping
        if(CurrentState == PlayerStates.InAir)
        {
            if(FlapsLeft > 0)
            {
                if(InAirTimer > TimeBtwFlaps)
                    FlapUp();
            }
            return;
        }

        if (CannotJump)
            return;

        //return from any extra states
        if (CurrentState != PlayerStates.Grounded)
            return;

        //we are now in the air
        SetInAir();

        //reduce our velocity on the y axis so our jump force can be added
        Vector3 VelAmt = Rigid.velocity;
        //clamp velocity
        VelAmt.y = 0;
        Rigid.velocity = VelAmt;
        //add our jump force
        Vector3 ForceAmt = (Vector3.up * JumpHeight) + (transform.forward * JumpForwardMod * CurrentMag);
        if(JumpBasedOnTransform)
            ForceAmt = (transform.up * JumpHeight) + (transform.forward * JumpForwardMod * CurrentMag);
        Rigid.AddForce(ForceAmt, ForceMode.Impulse);

        //create jumpfx
        Visual.Jump();
    }

    void ExitWater()
    {
        if (CannotJump)
            return;

        //we are now in the air
        SetInAir();

        //reduce our velocity on the y axis so our jump force can be added
        Vector3 VelAmt = Rigid.velocity;
        //clamp velocity
        VelAmt.y = 0;
        Rigid.velocity = VelAmt;
        //add our jump force
        Vector3 ForceAmt = (Vector3.up * WaterExitHeight);
        if (JumpBasedOnTransform)
            ForceAmt = (transform.up * WaterExitHeight);
        Rigid.AddForce(ForceAmt, ForceMode.Impulse);
    }

    void SwimUp()
    {
        if (CannotSwim)
            return;

        //cannot jump too frequently
        if (WaterTimer <= TimeBtwSwimJump)
            return;
        WaterTimer = 0;
        //reduce our velocity on the y axis so our jump force can be added
        Vector3 VelAmt = Rigid.velocity;
        //clamp velocity
        VelAmt.y = 0;
        Rigid.velocity = VelAmt;
        //add our jump force
        Vector3 ForceAmt = (Vector3.up * SwimHeight) + (transform.forward * SwimForward * CurrentMag);
        if (JumpBasedOnTransform)
            ForceAmt = (transform.up * SwimHeight) + (transform.forward * SwimForward * CurrentMag);
        Rigid.AddForce(ForceAmt, ForceMode.Impulse);

        //create swimfx
        Visual.Swim();

        //swim up 
        if (Anim)
            Anim.SetTrigger("Swim");
    }

    void FlapUp()
    {
        //reduce flap
        FlapsLeft -= 1;
        //reset timer
        InAirTimer = 0;

        //reduce our velocity on the y axis so our jump force can be added
        Vector3 VelAmt = Rigid.velocity;
        //clamp velocity
        VelAmt.y = 0;
        Rigid.velocity = VelAmt;
        //add our jump force
        Vector3 ForceAmt = (transform.up * FlapHeight);

        Rigid.AddForce(ForceAmt, ForceMode.Impulse);

        //create jumpfx
        Visual.Flap();
    }

    public void GrabForwards()
    {
        //if we are being held we cannot try to grab!
        if (CurrentState == PlayerStates.Held)
            return;
        if (CurrentState == PlayerStates.WaterSwimming)
            return;

        if (!CanGrab)
            return;

        if(HeldObj)
        {
            //throw the held object instead,
            ThrowObj(ActSpeed);
            return;
        }

        //check for a grabbable object
        GameObject GrabCheck = Coli.CheckGrab();
        if (GrabCheck == null)
            return;
        //check for room to grab
        bool RoofRoom = Coli.CheckRoof(transform.position);
        //no room above our head
        if (!RoofRoom)
            return;

        //check the object is not already grabbed
        PlayerMovement Ply = GrabCheck.GetComponent<PlayerMovement>();
        bool CanPickup = false;

        if(Ply)
        {
            //the object is already held, return
            bool Check = Ply.CanBeGrabbedCheck();
            if(Check)
            {
                Ply.SetGrabbed(this);
                CanPickup = true;
            }
        }
        else
        {
            //add logic for other pickups 
        }

        if (CanPickup)
            SetHolding(GrabCheck);
    }

    //check for if we can be picked up
    public bool CanBeGrabbedCheck()
    {
        if (CurrentState == PlayerStates.Held) //we are already held, cannot be picked up
            return false;
        else if (HeldObj != null) //this object is holding an object already and cannot be picked up
            return false;
        else if (!CanBeGrabbed)
            return false;

        return true; //we are free to be grabbed
    }

    public void ThrowObj(float Spd)
    {
        PlayerMovement Ply = HeldObj.GetComponent<PlayerMovement>();
        //get force to throw
        float ForwardsAmt = ThrowForceForwards;
        float UpAmt = ThrowForce;
        if(Spd <= 1.5)
        {
            ForwardsAmt = DropForceForwards;
            UpAmt = DropForce;
        }

        if (Ply)
        {
            //throw object
            Ply.Thrown(ForwardsAmt, UpAmt);
        }
        else
        {
            //add logic for other pickups 
        }

        //remove held object
        HeldObj = null;
        //turn off grabbing collider
        GrabbingCollider.enabled = false;
        //animation
        if (Anim)
            Anim.SetBool("Holding", false);
    }

    //we have been thrown from the object holding us
    public void Thrown(float ForwardAmt, float UpwardsAmt)
    {
        //have to be held
        if (CurrentState != PlayerStates.Held)
            return;

        //make our rigidbody not kinematic again
        Rigid.isKinematic = false;
        //make our collider normal
        OurCollider.isTrigger = false;
        //no longer being held
        HoldingUsObj = null;

        //add force
        Vector3 ForceDir = (transform.forward * ForwardAmt) + (transform.up * UpwardsAmt);
        Rigid.AddForce(ForceDir, ForceMode.Impulse);

        //we are now in the air!
        CurrentState = PlayerStates.InAir;
    }

    //we have been grabbed
    public void SetGrabbed(PlayerMovement Grabber)
    {
        //set what is holding us
        HoldingUsObj = Grabber;
        //make our rigidbody kinematic
        Rigid.isKinematic = true;
        //kill speed and velocity
        ActSpeed = 0;
        Rigid.velocity = Vector3.zero;
        //remove all our MovAdjustment
        MovAdjustment = 0;
        //make our collider a trigger (so we detect collision but do not interfier with movement
        OurCollider.isTrigger = true;
        //we are now held!
        CurrentState = PlayerStates.Held;
    }
    //for when we grab an object
    public void SetHolding(GameObject Held)
    {
        //we are now holding the held object
        HeldObj = Held;
        //set our grabbing collider to true
        GrabbingCollider.enabled = true;
        //animation
        if (Anim)
            Anim.SetBool("Holding", true);
    }

    float CheckDis(float D)
    {
        if (SpeedCheckTime <= 0)
        {
            SpeedCheckTime = TimeBtwSpeedChecks;
            LastPosition = transform.position;
        }
        else
            SpeedCheckTime -= D;

        float Dis = Vector3.Distance(transform.position, LastPosition);

        if (Dis <= DistanceNeeded)
            return 0f;

        return 1;
    }

    public void Damage()
    {
        if (CurrentState == PlayerStates.Static)
            return;

        //throw all held objects
        if(HeldObj)
        {
            PlayerMovement ply = HeldObj.GetComponent<PlayerMovement>();

            if (ply)
                ply.Thrown(ThrowForce, ThrowForceForwards);
            else
            {
                Debug.Log("Add other object throwing");
            }
        }
        if (HoldingUsObj)
            HoldingUsObj.SetHolding(null);
        //create fx
        Visual.Death();
        //remove from list
        GameCtrl.RemovePlayer(this);
        //destroy this object
        Destroy(this.gameObject);
    }
    //to destroy this unit with no effects
    public void RemoveUnit()
    {
        //throw all held objects
        if (HeldObj)
            ThrowObj(ActSpeed);
        if (HoldingUsObj)
            HoldingUsObj.SetHolding(null);
        //remove from list
        GameCtrl.RemovePlayer(this);
        //destroy this object
        Destroy(this.gameObject);
    }

    public void PowerUpTransform(int Power)
    {
        //if the player has already been activated, return
        if (CurrentState == PlayerStates.Static)
            return;
        CurrentState = PlayerStates.Static;

        //remove our collider for any glitches
        OurCollider.isTrigger = true;
        //create this power player
        GameCtrl.PowerPlayer(Power, this);
        //remove this unit and its reference
        RemoveUnit();
    }
}
