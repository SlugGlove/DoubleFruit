using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Colorful;

public class CameraCtrl : MonoBehaviour
{
    private float YTurn; //how much we have turned left and right
    private float ActYTurn; //how much we have turned left and right
    //private float XTurn; //how much we have turned Up or Down
    //private float ActXTurn; //how much we have turned Up or Down

    public float MouseSpeed;
    public float ControllerSpeed;

    public float LookLeftRightSpeed;
    public float LookUpSpeed; //how fast we look up and down

    public Transform NormalPos;
    public Transform HighPos;
    public Transform LowPos;

    public float Smoothing;
    private Camera Cam;


    public void Start()
    {
        Cam = GetComponentInChildren<Camera>();

        YTurn = 50;
        ActYTurn = 50;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //get inputs
        float Del = Time.deltaTime;
        float CamX = Input.GetAxis("Mouse X");
        float CamY = Input.GetAxis("Mouse Y");
        float Speed = MouseSpeed;
        if(CamX == 0 && CamY == 0)
        {
            CamX = Input.GetAxis("Controller X");
            CamY = Input.GetAxis("Controller Y");
            Speed = ControllerSpeed;
        }

        TurnLeftRight(CamX, Del, Speed);
        TurnUp(CamY, Del, Speed);
    }

    void TurnLeftRight(float X, float D, float spd)
    {
        Vector3 Dir = -transform.right;
        if (X > 0)
            Dir = transform.right;
        else
            X = X * -1;

        float singleStep = ((X * Time.deltaTime) * spd) * LookLeftRightSpeed;

        // Rotate the forward vector towards the target direction by one step
        Vector3 newDirection = Vector3.RotateTowards(transform.forward, Dir, singleStep, 0.0f);

        // Calculate a rotation a step closer to the target and applies rotation to this object
        Quaternion Ang = Quaternion.LookRotation(newDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, Ang, Smoothing * D);
    }
    void TurnUp(float Y, float D, float Spd)
    {
        YTurn -= (Y * LookUpSpeed * Spd) * D;
        YTurn = Mathf.Clamp(YTurn, 0, 100);
        ActYTurn = Mathf.Lerp(ActYTurn, YTurn, D * Smoothing);

        float LAmt = ActYTurn / 100;
        if (LAmt < 0.5)
        {
            LAmt = LAmt * 2;

            Cam.transform.position = Vector3.Lerp(LowPos.position, NormalPos.position, LAmt);
            Cam.transform.localRotation = Quaternion.Slerp(LowPos.localRotation, NormalPos.localRotation, LAmt);
        }
        else
        {
            LAmt = (LAmt - 0.5f) * 2;

            Cam.transform.position = Vector3.Lerp(NormalPos.position, HighPos.position, LAmt);
            Cam.transform.localRotation = Quaternion.Slerp(NormalPos.localRotation, HighPos.localRotation, LAmt);
        }
    }

    /*
    public void FovHandle(float D, float Vel)
    {
        //get appropritate fov 
        float LerpAmt = (Vel - FovMinSpeed) / FOVSpeed;
        float FieldView = Mathf.Lerp(MinFov, MaxFov, LerpAmt);
        //ease into this fov
        Cam.fieldOfView = Mathf.Lerp(Cam.fieldOfView, FieldView, 4 * D);

        //handle blue 
        float SecondLerp = (Vel - (FovMinSpeed * 1.2f)) / (FOVSpeed * 1.2f);
        float BlurAmt = Mathf.Lerp(0, -0.5f, SecondLerp);
        BlurFx.Distortion = Mathf.Lerp(BlurFx.Distortion, BlurAmt, 7 * D);
    }
    */
}
