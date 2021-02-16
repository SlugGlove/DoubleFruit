using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    private PlayerMovement Mover;
    public float FloorSize; //how large the floor check is
    public float FloorDis; //how large the detection for the floors is
    public float bottomOffset; //offset from player centre
    public float WaterOffset; //offset from the player bottom
    public float TopOffset; //offset from the top of us
    public float RoofSize;
    public float WaterSize;

    public LayerMask FloorLayers; //what layers we can stand on
    public LayerMask RoofLayers;
    public LayerMask UnitLayers; //what layers other units of this type are, for grabbing
    public LayerMask GrabableLayers; //what we can grab
    public LayerMask WaterLayer; //what layer the water is

    private void Start()
    {
        Mover = GetComponent<PlayerMovement>();
    }

    //check if there is floor below us
    public Vector3 CheckFloor(Vector3 Direction)
    {
        Vector3 Pos = transform.position + (Direction * bottomOffset);
        Collider[] hitColliders = Physics.OverlapSphere(Pos, FloorSize, FloorLayers);
        if (hitColliders.Length > 0)
        {
            //check we are not just colliding with ourself

            //we are on the ground get an angle
            RaycastHit hit;
            if (Physics.Raycast(transform.position + (-transform.up * (bottomOffset - 0.3f)), Direction, out hit, FloorDis, FloorLayers))
            {
                //we are on the ground
                return hit.normal;
            }

            //we hit the floor but missed a check, return up
            return Vector3.up;
        }

        //if there is no floor, check if another player is below us
        Collider[] PlayerCol = Physics.OverlapSphere(Pos, FloorSize - 0.1f, UnitLayers);
        if (PlayerCol.Length > 0)
        {
            bool SelfCol = true;
            foreach(Collider Col in PlayerCol)
            {
                if(Col.gameObject != this.gameObject) //if we are not colliding with ourself
                {
                    SelfCol = false;
                    break;
                }
            }
            if (SelfCol)
            {
                return Vector3.zero;
            }
            else
            {
                //we hit a player, return up
                return Vector3.up;
            }
        }

        return Vector3.zero;
    }

    public Vector3 FloorAngle()
    {
        //we are on the ground get an angle
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -Vector3.up, out hit, 10f, FloorLayers))
        {
            //we are on the ground
            return hit.normal;
        }

        //we hit the floor but missed a check, return up
        return Vector3.up;
    }

    public bool CheckRoof(Vector3 OriginPos)
    {
        Vector3 CheckPos = OriginPos + (transform.up * TopOffset);

        Collider[] hitColliders = Physics.OverlapSphere(CheckPos, RoofSize, RoofLayers);
        if (hitColliders.Length > 0)
        {
            //hit something above us! cannot pickup
            return false;
        }

        //nothing above us, it is okey to pickup
        return true;
    }

    public GameObject CheckGrab()
    {
        Collider[] PlayerCol = Physics.OverlapSphere(transform.position + (transform.forward * 1f), 0.2f, GrabableLayers);
        if (PlayerCol.Length > 0)
        {
            GameObject Gotcha = null;
            foreach (Collider Col in PlayerCol)
            {
                if (Col.gameObject != this.gameObject) //if we are not colliding with ourself
                {
                    Gotcha = Col.gameObject;
                    break;
                }
            }
            //return what we grabbed
            return Gotcha;
        }


        //nothing to grab
        return null;
    }

    public bool CheckWater()
    {
        Collider[] PlayerCol = Physics.OverlapSphere(transform.position + (transform.up * WaterOffset), WaterSize, WaterLayer);
        if (PlayerCol.Length > 0)
        {
            //we are underwater
            return true;
        }

        return false;
    }

    void OnDrawGizmosSelected()
    {
        //floor check
        Gizmos.color = Color.red;
        Vector3 Pos = transform.position + (-transform.up * bottomOffset);
        Gizmos.DrawLine(transform.position, Pos + (-transform.up * FloorDis));
        //floor check
        Gizmos.color = Color.red;
        Pos = transform.position + (-transform.up * bottomOffset);
        Gizmos.DrawSphere(Pos,FloorSize);
        //roof check
        Gizmos.color = Color.green;
        Vector3 Pos5 = transform.position + (transform.up * TopOffset);
        Gizmos.DrawSphere(Pos5, RoofSize);
    }
}
