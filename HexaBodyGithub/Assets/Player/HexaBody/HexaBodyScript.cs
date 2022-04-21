using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine.XR;
using UnityEngine.InputSystem;

public class HexaBodyScript : MonoBehaviour
{
    [Header("XR Toolkit Parts")]
    public GameObject XRRig;
    public GameObject XRCamera;

    [Header("Actionbased Controller")]
    public ActionBasedController CameraController;
    public ActionBasedController RightHandController;
    public ActionBasedController LeftHandController;

    public InputActionReference RightTrackPadPressed;
    public InputActionReference RightTrackPadTouch;

    public InputActionReference LeftTrackPadClick;

    [Header("Hexabody Parts")]
    public GameObject Head;
    public GameObject Chest;
    public GameObject Fender;
    public GameObject Monoball;


    public ConfigurableJoint Spine;

    [Header("Hexabody Movespeed")]
    public float moveForceCrouch;
    public float moveForceWalk;
    public float moveForceSprint;

    [Header("Hexabody Drag")]
    public float angularDragOnMove;
    public float angularBreakDrag;

    [Header("Hexabody Croch & Jump")]
    bool jumping = false;

    public float crouchSpeed;
    public float lowesCrouch;
    public float highestCrouch;
    private float additionalHight;

    Vector3 CrouchTarget;

    //---------Input Values---------------------------------------------------------------------------------------------------------------//

    private Quaternion headYaw;
    private Vector3 moveDirection;
    private Vector3 monoballTorque;

    private Vector3 CameraControllerPos;

    private Vector3 RightHandControllerPos;
    private Vector3 LeftHandControllerPos;

    private Quaternion RightHandControllerRotation;
    private Quaternion LeftHandControllerRotation;

    private Vector2 RightTrackpad;
    private Vector2 LeftTrackpad;

    private float RightTrackpadPressed;
    private float leftTrackpadPressed;

    private float RightTrackpadTouched;

    void Start()
    {
        additionalHight = (0.5f * Monoball.transform.lossyScale.y) + (0.5f * Fender.transform.lossyScale.y) + (Head.transform.position.y - Chest.transform.position.y);
    }


    void Update()
    {
        CameraToPlayer();
        XRRigToPlayer();

        getContollerInputValues();
    }

    private void FixedUpdate()
    {
        movePlayerViaController();
        jump();

        if (!jumping)
        {
            spineContractionOnRealWorldCrouch();
        }

        rotatePlayer();
     
    }

    private void getContollerInputValues()
    {
        //Right Controller
        //Position & Rotation
        RightHandControllerPos = RightHandController.positionAction.action.ReadValue<Vector3>();
        RightHandControllerRotation = RightHandController.rotationAction.action.ReadValue<Quaternion>();

        //Trackpad
        RightTrackpad = RightHandController.translateAnchorAction.action.ReadValue<Vector2>();
        RightTrackpadPressed = RightTrackPadPressed.action.ReadValue<float>();
        RightTrackpadTouched = RightTrackPadTouch.action.ReadValue<float>();

        //Left Contoller
        //Position & Rotation
        LeftHandControllerPos = LeftHandController.positionAction.action.ReadValue<Vector3>();
        LeftHandControllerRotation = LeftHandController.rotationAction.action.ReadValue<Quaternion>();

        //Trackpad
        LeftTrackpad = LeftHandController.translateAnchorAction.action.ReadValue<Vector2>();
        leftTrackpadPressed = LeftTrackPadClick.action.ReadValue<float>();

        //Camera Inputs
        CameraControllerPos = CameraController.positionAction.action.ReadValue<Vector3>();

        headYaw = Quaternion.Euler(0, XRCamera.transform.eulerAngles.y, 0);
        moveDirection = headYaw * new Vector3(RightTrackpad.x, 0, RightTrackpad.y);
        monoballTorque = new Vector3(moveDirection.z, 0, -moveDirection.x);
    }

    //------Transforms---------------------------------------------------------------------------------------
    private void CameraToPlayer()
    {
        XRCamera.transform.position = Head.transform.position;
    }
    private void XRRigToPlayer()
    {
        XRRig.transform.position = new Vector3(Fender.transform.position.x, Fender.transform.position.y - (0.5f * Fender.transform.localScale.y + 0.5f * Monoball.transform.localScale.y), Fender.transform.position.z);
    }
    private void rotatePlayer()
    {
        Chest.transform.rotation = headYaw;
    }
    //-----HexaBody Movement---------------------------------------------------------------------------------
    private void movePlayerViaController()
    {
        if (!jumping)
        {
            if (RightTrackpadTouched == 0)
            {
                stopMonoball();
            }

            else if (RightTrackpadPressed == 0 && RightTrackpadTouched == 1)
            {
                moveMonoball(moveForceWalk);
            }

            else if (RightTrackpadPressed == 1)
            {
                moveMonoball(moveForceSprint);
            }
        }

        else if (jumping)
        {
            if (RightTrackpadTouched == 0)
            {
                stopMonoball();
            }

            else if (RightTrackpadTouched == 1)
            {
                moveMonoball(moveForceCrouch);
            }
        }

    }
    private void moveMonoball(float force)
    {
        Monoball.GetComponent<Rigidbody>().freezeRotation = false;
        Monoball.GetComponent<Rigidbody>().angularDrag = angularDragOnMove;
        Monoball.GetComponent<Rigidbody>().AddTorque(monoballTorque.normalized * force, ForceMode.Force);
    }
    private void stopMonoball()
    {
        Monoball.GetComponent<Rigidbody>().angularDrag = angularBreakDrag;

        if (Monoball.GetComponent<Rigidbody>().velocity == Vector3.zero)
        {
            Monoball.GetComponent<Rigidbody>().freezeRotation = true;
        }

    }

    //------Jumping------------------------------------------------------------------------------------------
    private void jump()
    {
        if (leftTrackpadPressed == 1 && LeftTrackpad.y < 0)
        {
            jumping = true;
            jumpSitDown();
        }

        else if ((leftTrackpadPressed == 0) && jumping == true)
        {
            jumping = false;
            jumpSitUp();
        }

    }
    private void jumpSitDown()
    {
        if (CrouchTarget.y >= lowesCrouch)
        {
            CrouchTarget.y -= crouchSpeed * Time.fixedDeltaTime;
            Spine.targetPosition = new Vector3(0, CrouchTarget.y, 0);
        }
    }
    private void jumpSitUp()
    {
        CrouchTarget = new Vector3(0, highestCrouch - additionalHight, 0);
        Spine.targetPosition = CrouchTarget;
    }

    //------Joint Controll-----------------------------------------------------------------------------------
    private void spineContractionOnRealWorldCrouch()
    {
        CrouchTarget.y = Mathf.Clamp(CameraControllerPos.y - additionalHight, lowesCrouch, highestCrouch - additionalHight);
        Spine.targetPosition = new Vector3(0, CrouchTarget.y, 0);

    }

}
