using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    private GamePlayer Ctrl;
    private CameraCtrl Cam;
    public GameObject[] PlayerObj;

    private void Start()
    {
        Ctrl = GetComponent<GamePlayer>();
        Cam = GetComponentInChildren<CameraCtrl>();
    }

    //create our initial player
    public void SpawnPlayer()
    {
        if (Ctrl.Active.Count >= 100) //cannot pass limit
            return;

        //create a gameobject of the player
        GameObject PLY = Instantiate(PlayerObj[0], transform.position, Quaternion.identity);
        //add this player to the list
        PlayerMovement PlyMov = PLY.GetComponent<PlayerMovement>();
        Ctrl.AddPlayer(PlyMov);
        //setup the player
        PlyMov.Setup(Cam, Ctrl);
    }

    public void PowerPlayer(int Type, Transform Rot)
    {
        //create a gameobject of the player
        GameObject Spawner = PlayerObj[Type];
        //create at former position and rotation
        GameObject PLY = Instantiate(Spawner, Rot.transform.position, Rot.transform.rotation);
        //carry the former players momentum
        Debug.Log("carry the former players momentum");
        //add this player to the list
        PlayerMovement PlyMov = PLY.GetComponent<PlayerMovement>();
        Ctrl.AddPlayer(PlyMov);
        //setup the player
        PlyMov.Setup(Cam, Ctrl);
    }
}
