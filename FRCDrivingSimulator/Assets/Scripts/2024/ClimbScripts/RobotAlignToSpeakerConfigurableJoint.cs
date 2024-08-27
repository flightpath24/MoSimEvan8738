using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RobotAlignToSpeakerConfigurableJoint : MonoBehaviour
{
    // Start is called before the first frame update
   
    [SerializeField] private Alliance alliance;

    [SerializeField] private ConfigurableJoint ShooterPivot;

    [SerializeField] private RobotNoteManager NoteManager;

    public Rigidbody robotRigidbody;
    public float robotRotationSpeed = 50f;

    public GameObject indicator;

    public Transform target;

    public float MaxAimDistance = 40;

    public bool noteDropZone;

    public float noteDropRange;

    public float noteDropRangeMax;

    public float noteDropSpeed;

    public float angleOffset = 0;

    public float downwardOffset = 0;

    private Vector3 PivotStartingPos;
    private Quaternion PivotStartingRot;
    private int startingLayer;

    private bool climb = false;

    private bool amp = false;

    private bool intake = false;

    public float targetAngle = 0;

    public float ampRotation = 0;

    public float PassAngle = 0;

    public float distanceToTarget;

    public float ampDuration; 
    public float ampPeriod = 0;
    private bool isAmping;

    public bool hasStowZones;
    public Collider BlueStowZone;
	public Collider RedStowZone;
	public Collider RobotCollider;

    private DriveController drive;

    public bool isShooting = false;

    private Quaternion targetRotation;

    private float shoot;

    private bool canDoAlign;

    // Start is called before the first frame update
    void Start()
    {

        PivotStartingPos = ShooterPivot.gameObject.transform.localPosition;
        PivotStartingRot = ShooterPivot.gameObject.transform.localRotation;

        drive = gameObject.GetComponent<DriveController>();
    }

    // Update is called once per frame
    void Update()
    {
        distanceToTarget = Vector3.Distance(ShooterPivot.transform.position, target.position);

        Quaternion targetShooterRotation = Quaternion.LookRotation( target.position - Vector3.up * downwardOffset - indicator.transform.position, Vector3.up);
        Vector3 euler = targetShooterRotation.eulerAngles;
    if (GameManager.canRobotMove) {
        if (hasStowZones &&!intake && !climb && !(ampPeriod>0) &&(BlueStowZone.bounds.Intersects(RobotCollider.bounds) || RedStowZone.bounds.Intersects(RobotCollider.bounds))) {
            targetAngle = 0;
        } else if (climb) {
            targetAngle = 90;
        } else if (!climb && !amp && !(ampPeriod > 0) && !intake && distanceToTarget < MaxAimDistance){
            NoteManager.speed = NoteManager.shootingSpeed;
            targetAngle = euler.x + angleOffset;
        } else if (intake && !amp && !climb){
            targetAngle = 0;
            canDoAlign = false;
        } else if (ampPeriod > 0 && !intake && !climb) {
            isAmping = true;
            targetAngle = ampRotation;
            canDoAlign = false;
            
        } else if (distanceToTarget > MaxAimDistance){
            NoteManager.speed = NoteManager.passingSpeed;
            targetAngle = PassAngle;

        } 

        if (noteDropZone && distanceToTarget > noteDropRange && distanceToTarget < noteDropRangeMax && distanceToTarget < MaxAimDistance) {
            NoteManager.speed = noteDropSpeed;
        }


        if (shoot > 0 && canDoAlign && distanceToTarget <= MaxAimDistance) {
            canDoAlign = false;
            RotateRobotToTarget();
        } else if (shoot == 0f) {
            canDoAlign = true;
        }
            
    } else {
        
    }
        if (amp && !isAmping) {
            ampPeriod = ampDuration;
        } else if (ampPeriod > -0.1) {
            ampPeriod = ampPeriod - Time.deltaTime;
        } else {
             isAmping = false;
        }

        ShooterPivot.targetRotation = Quaternion.Euler(-targetAngle, 0, 0);

    }

    public void RotateRobotToTarget()
    {
        if (!isShooting && drive.isGrounded)
        {
            isShooting = true;

            Vector3 directionToTarget = target.position - transform.position;
            directionToTarget.y = 0f;

            targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);

            StartCoroutine(RotateTowardsTarget(targetRotation));
        }
    }

    private IEnumerator RotateTowardsTarget(Quaternion targetRotation)
    {
        if (alliance == Alliance.Blue) { DriveController.canBlueRotate = false; }
        else { DriveController.canRedRotate = false; }

        while (Quaternion.Angle(robotRigidbody.rotation, targetRotation) > 0.1f)
        {
            robotRigidbody.rotation = Quaternion.RotateTowards(robotRigidbody.rotation, targetRotation, robotRotationSpeed * Time.deltaTime);
            yield return null;
        }

        if (alliance == Alliance.Blue) { DriveController.canBlueRotate = true; }
        else { DriveController.canRedRotate = true; }

        isShooting = false;
    }

    private IEnumerator WaitToEnable() 
    {
        yield return new WaitForSeconds(0.01f);
        ShooterPivot.gameObject.layer = startingLayer;
    }

    public void OnAmp(InputAction.CallbackContext ctx)
    {
        amp = ctx.action.triggered;
    }

    public void OnIntake(InputAction.CallbackContext ctx)
    {
        intake = ctx.action.triggered;
    }

    public void OnShoot(InputAction.CallbackContext ctx)
    {
        shoot =  ctx.ReadValue<float>();
    }

    public void OnClimb(InputAction.CallbackContext ctx)
    {
        climb =  ctx.action.triggered;
    }

    public void Reset() 
    {
        
        ShooterPivot.gameObject.layer = 17;


        //Reset joints pos and rot and targetPos
        ShooterPivot.gameObject.transform.localPosition = PivotStartingPos;
        ShooterPivot.gameObject.transform.localRotation = PivotStartingRot;

        ShooterPivot.targetRotation = Quaternion.Euler(0, 0, 0);

        StartCoroutine(WaitToEnable());
    }
}