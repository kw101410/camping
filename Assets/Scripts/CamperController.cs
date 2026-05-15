using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CamperController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Wheel Transforms")]
    public Transform frontLeftTransform;
    public Transform frontRightTransform;
    public Transform rearLeftTransform;
    public Transform rearRightTransform;

    [Header("Vehicle Settings")]
    public float motorForce = 1500f;
    public float maxSteerAngle = 30f;
    public float brakeForce = 3000f;

    [Header("Chaos Physics (억까 물리 설정)")]
    [Tooltip("무게 중심을 높이면 차가 쉽게 전복됩니다. 현재는 절대 좌표(Y=0.2)로 낮게 고정되어 있습니다.")]
    public Vector3 centerOfMassOffset = new Vector3(0, 0.2f, 0);

    [Header("Mini-Game: Cargo Balance (Player 2)")]
    [Tooltip("조수석 플레이어 시각 모델 (할당하면 좌우로 움직입니다)")]
    public Transform passengerModel;
    [Tooltip("조수석 플레이어(J, L 키)가 무게 중심을 좌우로 이동시킬 수 있는 최대 범위")]
    public float maxWeightShift = 0.8f;
    [Tooltip("무게 중심 이동 속도")]
    public float weightShiftSpeed = 3f;

    private float currentWeightShift = 0f;

    private float horizontalInput;
    private float verticalInput;
    private bool isBraking;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 차체(Body) 콜라이더가 높게 있어 기본 무게 중심이 너무 높은 문제를 해결하기 위해 절대값으로 고정
        rb.centerOfMass = centerOfMassOffset;
    }

    void Update()
    {
        HandleWeightBalance();
        GetInput();
        UpdateWheelVisuals();
    }

    private void HandleWeightBalance()
    {
        if (Keyboard.current == null) return;

        float targetShift = 0f;
        // J, L 키로 조수석 플레이어가 무게 중심을 좌우로 이동
        if (Keyboard.current.lKey.isPressed) targetShift = maxWeightShift;   // 우측으로 체중 싣기
        if (Keyboard.current.jKey.isPressed) targetShift = -maxWeightShift;  // 좌측으로 체중 싣기

        // 부드럽게 무게 중심 이동
        currentWeightShift = Mathf.Lerp(currentWeightShift, targetShift, Time.deltaTime * weightShiftSpeed);
        
        // Rigidbody의 실시간 무게 중심에 반영 (원래 오프셋 + 좌우 이동값)
        rb.centerOfMass = centerOfMassOffset + new Vector3(currentWeightShift, 0, 0);

        // 시각 모델(빨간 캡슐) 이동 (실제로 차 안에서 움직이는 것처럼 연출)
        if (passengerModel != null)
        {
            Vector3 pos = passengerModel.localPosition;
            pos.x = currentWeightShift;
            passengerModel.localPosition = pos;
        }
    }

    void FixedUpdate()
    {
        HandleMotor();
        HandleSteering();
    }

    private void GetInput()
    {
        if (Keyboard.current != null)
        {
            float right = Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1f : 0f;
            float left = Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed ? 1f : 0f;
            horizontalInput = right - left;

            float up = Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed ? 1f : 0f;
            float down = Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed ? 1f : 0f;
            verticalInput = up - down;

            isBraking = Keyboard.current.spaceKey.isPressed;
        }
        else
        {
            horizontalInput = 0f;
            verticalInput = 0f;
            isBraking = false;
        }
    }

    private void HandleMotor()
    {
        float acceleration = verticalInput * motorForce;
        
        // 사륜구동 설정 (또는 후륜구동으로 변경 가능)
        frontLeftWheel.motorTorque = acceleration;
        frontRightWheel.motorTorque = acceleration;
        rearLeftWheel.motorTorque = acceleration;
        rearRightWheel.motorTorque = acceleration;

        float currentBrakeForce = isBraking ? brakeForce : 0f;
        ApplyBraking(currentBrakeForce);
    }

    private void ApplyBraking(float force)
    {
        frontLeftWheel.brakeTorque = force;
        frontRightWheel.brakeTorque = force;
        rearLeftWheel.brakeTorque = force;
        rearRightWheel.brakeTorque = force;
    }

    private void HandleSteering()
    {
        float currentSteerAngle = maxSteerAngle * horizontalInput;
        frontLeftWheel.steerAngle = currentSteerAngle;
        frontRightWheel.steerAngle = currentSteerAngle;
    }

    private void UpdateWheelVisuals()
    {
        UpdateSingleWheel(frontLeftWheel, frontLeftTransform);
        UpdateSingleWheel(frontRightWheel, frontRightTransform);
        UpdateSingleWheel(rearLeftWheel, rearLeftTransform);
        UpdateSingleWheel(rearRightWheel, rearRightTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        if (wheelTransform == null) return;

        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.position = pos;
        
        // 기본 실린더 형태를 바퀴로 썼기 때문에, 
        // 휠 콜라이더의 회전값에 Z축 90도 회전을 더해 시각적 방향을 맞춰줍니다.
        wheelTransform.rotation = rot * Quaternion.Euler(0, 0, 90f);
    }
}
