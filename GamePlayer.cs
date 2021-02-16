using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayer : MonoBehaviour
{
    public List<PlayerMovement> Active = new List<PlayerMovement>();
    private PlayerSpawner Spawn;
    private bool HeldInput;

    [Header("Following")]
    public float FollowSpd;
    public float MaxY;
    public float MinY;

    private bool GrabbingFunc;

    // Start is called before the first frame update
    void Start()
    {
        Spawn = GetComponent<PlayerSpawner>();
    }

    private void Update()
    {
        float GrbDup = Input.GetAxis("Duplicate");

        //get jump input
        bool Jump = Input.GetButtonDown("Jump");
        bool Grab = Input.GetButtonDown("Grab");

        if (GrbDup == 0)
            HeldInput = false;
        else if (GrbDup > 0) //duplicate our player
        {
            if (!HeldInput)
            {
                HeldInput = true; //we have held an input
                Spawn.SpawnPlayer(); //create a player
            }
        }

        //move controller
        if (Active.Count <= 0)
            return;

        //if we are trying to grab or jump
        if (Jump)
        {
            PlayerJump();
        }
        else if (Grab)
        {
            if(!GrabbingFunc)
            {
                GrabbingFunc = true;
                PlayerGrab();
            }
        }
    }

    void PlayerJump()
    {
        //have each player jump up
        foreach(PlayerMovement Mover in Active)
        {
            Mover.JumpUp();
        }
    }

    void PlayerGrab()
    {
        //have each player grab with a pause
        foreach (PlayerMovement Mover in Active)
        {
            Mover.GrabForwards();
           // yield return new WaitForSeconds(0.001f);
        }

        GrabbingFunc = false;
    }

    void MovePlayers(float D, PlayerMovement Mov, float H, float V)
    {
        Mov.Tick(D, H, V);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float Del = Time.deltaTime;

        //move controller
        if (Active.Count <= 0)
            return;

        //get inputs
        float horInput = Input.GetAxis("Horizontal");
        float verInput = Input.GetAxis("Vertical");

        //move all our players
        Vector3 RelPos = Vector3.zero;
        int RelAmt = 0;

        foreach (PlayerMovement Mover in Active)
        {
            MovePlayers(Del, Mover, horInput, verInput);

            RelAmt += 1;
            RelPos += Mover.transform.position;
        }
        //return if no positions? this shouldnt happen
        if (RelAmt == 0)
        {
            Debug.Log("No relative postitions for camera movement? all players are being held??");
            return;
        }
        //find mean of the relative positions of players
        RelPos = RelPos / RelAmt;

        MoveSelf(Del, RelPos);
    }

    void MoveSelf(float D, Vector3 Target)
    {
        Vector3 LerpPos = Target;
        LerpPos.y = Mathf.Clamp(LerpPos.y, MinY, MaxY);
        transform.position = Vector3.Lerp(transform.position, LerpPos, D * FollowSpd);
    }

    public void AddPlayer(PlayerMovement Mov)
    {
        Active.Add(Mov);
    }

    public void RemovePlayer(PlayerMovement Obj)
    {
        if (Active.Count <= 0)
            return;

        foreach(PlayerMovement Ply in Active)
        {
            if(Ply == Obj)
            {
                Active.Remove(Obj);
                return;
            }
        }
    }

    public void PowerPlayer(int Power, PlayerMovement FormerPlayer)
    {
        Debug.Log("fixed multiplying player bug");

        //remove former player
        RemovePlayer(FormerPlayer);

        //create this power
        Spawn.PowerPlayer(Power, FormerPlayer.transform);
    }

}
