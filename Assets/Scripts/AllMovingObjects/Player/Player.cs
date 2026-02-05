using UnityEngine;
using System.Collections;

enum AnimStateTest
{
    Idle,
    Run,
    Jump,
    Falling
}

public class Player : MovingObject
{
    private Animator anim;

    public GameObject playerModel;
    public GameObject itemSocket;

    [HideInInspector]
    public int Oxygen;


    private float currentAngle;

    private AnimStateTest animState;

    private bool inputFreeze = false;

    private float inputedX;
    private float inputedY;

    //protected bool isJumping = false;
    //protected bool isGrounded = true;


    public Vector3 inputedDir{ get; private set; }



    // 새롭게 추가한 컴포넌트 클래스
    private PlayerInput playerInput;
    private PlayerAnimator playerAnimator;
    private PlayerRaycastCollisionSystem playerCollisionControl;

    // 새롭게 리팩토링된 버전
    protected override void Start()
    {
        base.Start();

        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<PlayerAnimator>();
        playerCollisionControl = GetComponent<PlayerRaycastCollisionSystem>();

        playerAnimator.Initialize();

        groundMask = LayerMask.GetMask("Ground");
        wallMask = LayerMask.GetMask("Wall");
    }

    private void Update()
    {
        inputFreeze = playerCollisionControl.CollisionDetectWithRaycast(inputedDir, wallMask);

        if (!inputFreeze)
        {
            playerInput.InputProcess();

            playerCollisionControl.GroundDetectingWithRaycast(groundMask);

            playerAnimator.PlayerAnimation(playerInput.axisResultDir,
                playerInput.GetIsJumping(),
                playerCollisionControl.GetIsGrounded(),
                inputFreeze);

            AdjustAngle(playerInput.axisResultDir);
        }
    }

    private void FixedUpdate()
    {
        if (!inputFreeze)
        {
            Moving(playerInput.axisResultDir);

            if (playerInput.GetIsJumping())
            {
                Jump();
                playerInput.MakeIsJumpingFalse();
            }
        }
        else
        {
            rb.Sleep();
        }
    }

    //protected override void Start()
    //{
    //    base.Start();
    //    anim = gameObject.GetComponentInChildren<Animator>();
    //    Oxygen = 100;
    //    inputedDir = Vector3.zero;

    //    animState = new AnimStateTest();

    //    groundMask = LayerMask.GetMask("Ground");
    //    wallMask = LayerMask.GetMask("Wall");

    //    foreach (Transform child in this.transform.GetComponentsInChildren<Transform>(true))
    //    {
    //        if (child.name.Contains("Socket"))
    //        {
    //            itemSocket = child.gameObject;
    //            break;
    //        }
    //    }

    //    //StartCoroutine(OxygenSystem());
    //}

    //void Update()
    //{
    //    CollisionDetectWithRaycast();

    //    if (!inputFreeze)
    //    {
    //        PlayerInput();
    //        PlayerAnimation(inputedDir, isJumping, isGrounded);
    //        AdjustAngle(inputedDir);
    //    }
    //}

    //private void FixedUpdate()
    //{
    //    if (!inputFreeze)
    //    {
    //        Moving(inputedDir);

    //        if (isJumping)
    //        {
    //            Jump();
    //            isJumping = false;
    //        }
    //    }
    //    else
    //    {
    //        rb.Sleep();
    //    }
    //}

    // 인풋
    //private void PlayerInput()
    //{
    //    inputedX = Input.GetAxisRaw("Horizontal");
    //    inputedY = Input.GetAxisRaw("Vertical");
    //    inputedDir = new Vector3(inputedX, 0, inputedY).normalized;

    //    // 마우스 좌클릭으로 상호작용
    //    //bool isLeftClick = Input.GetMouseButton(0);

    //    // E키로 아이템 드랍
    //    //bool isPressE = Input.GetKeyDown(KeyCode.E);

    //    // 스페이스를 눌렀을 때. 아무런 문제가 없다면 이제부터 공중에 뜨게 됨
    //    if (Input.GetButtonDown("Jump"))
    //    {
    //        isJumping = true;
    //    }

    //    // 행성 방향으로 레이캐스트 발사 - 레이캐스트
    //    Vector3 origin = transform.position + transform.up * 0.5f;
    //    Ray ray = new Ray(origin, -transform.up);

    //    RaycastHit hit;

    //    Debug.DrawLine(ray.origin, ray.origin + ray.direction * (1.1f), Color.red);

    //    // 발이 땅에 닿았을 때를 감지
    //    if (Physics.Raycast(ray, out hit, 1.1f, groundMask))
    //    {
    //        isGrounded = true;
    //    }
    //    else
    //    {
    //        isGrounded = false;
    //    }


    //    //DropItem(isPressE);
    //}

    //// 애니메이션
    ////private void PlayerAnimation(Vector3 moveDir, bool isJumping, bool isGrounded, bool isLeftClickedData)
    //private void PlayerAnimation(Vector3 moveDir, bool isJumping, bool isGrounded)
    //{
    //    if (inputFreeze || isGrounded && moveDir == Vector3.zero)
    //    {
    //        //anim.SetInteger("AnimationPar", ANIM_IDLE);
    //        animState = AnimStateTest.Idle;
    //    }

    //    //else if (isLeftClickedData && isHavePickaxe)
    //    //{
    //    //    anim.SetInteger("AnimationPar", 3);
    //    //    return;
    //    //}

    //    else if (isJumping && isGrounded)
    //    {
    //        //anim.SetInteger("AnimationPar", ANIM_JUMP);
    //        animState = AnimStateTest.Jump;
    //    }

    //    else if (moveDir != Vector3.zero && isGrounded)
    //    {
    //        //anim.SetInteger("AnimationPar", ANIM_RUN);
    //        animState = AnimStateTest.Run;
    //    }

    //    else if (!isGrounded)
    //    {
    //        //anim.SetInteger("AnimationPar", ANIM_FALLING);
    //        animState = AnimStateTest.Falling;
    //    }


    //    anim.SetInteger("AnimationPar", (int)animState);
    //}

    //// 콜리전감지 - 레이캐스트
    //private void CollisionDetectWithRaycast()
    //{
    //    Vector3 rayTargetDir = transform.TransformDirection(inputedDir);

    //    Ray ray = new Ray(this.transform.position, rayTargetDir);
    //    RaycastHit hit;

    //    float rayLength = 2.1f;

    //    if (Physics.Raycast(ray, out hit, rayLength, wallMask))
    //    {
    //        inputFreeze = true;
    //    }
    //    else
    //    {
    //        inputFreeze = false;
    //    }

    //    Debug.DrawLine(ray.origin, ray.origin + ray.direction * (rayLength), Color.blue);
    //}

    //private IEnumerator OxygenSystem()
    //{
    //    while (true)
    //    {
    //        Oxygen -= 1;
    //        yield return new WaitForSeconds(1f);
    //    }
    //}

    // 각도조정 (이건 일단 그대로.)
    private void AdjustAngle(Vector3 moveDir)
    {
        if (moveDir != Vector3.zero)
        {
            float targetAngle = (Mathf.Atan2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")) * Mathf.Rad2Deg + 360f) % 360f;
            currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, 720f * Time.deltaTime);
        }

        playerModel.transform.localEulerAngles = new Vector3(-90f, 0f, currentAngle);
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (other.CompareTag("Item") && !isHavePickaxe && !isHaveItem)
    //    {
    //        Debug.Log("아이템 획득");

    //        isHavePickaxe = true;
    //        EquipItem(other);

    //    }

    //    if (other.CompareTag("HandledItem") && !isHavePickaxe && !isHaveItem)
    //    {
    //        isHaveItem = true;
    //        EquipItem(other);
    //    }

    //    if (other.CompareTag("Resource"))
    //    {
    //        Debug.Log("자원 획득");
    //        Destroy(other.gameObject);
    //        gold++;
    //    }

    //    if (other.CompareTag("Spaceship"))
    //    {
    //        Debug.Log("우주선 접촉");
    //    }
    //}

    //private void EquipItem(Collider other)
    //{
    //    PlanetGravityForObjects pgfo = other.GetComponent<PlanetGravityForObjects>();
    //    pgfo.enabled = false;
    //    Rigidbody rb = other.GetComponent<Rigidbody>();
    //    rb.isKinematic = true;

    //    other.transform.SetParent(itemSocket.transform);

    //    BoxCollider[] colliders = other.GetComponentsInChildren<BoxCollider>();
    //    foreach (var col in colliders)
    //    {
    //        col.enabled = false;
    //    }

    //    other.transform.localPosition = Vector3.zero;
    //    other.transform.localRotation = Quaternion.identity;
    //}

    //private void DropItem(bool inputData)
    //{
    //    // 곡괭이거나 광석인 경우
    //    if (inputData && isHavePickaxe || inputData && isHaveItem)
    //    {
    //        isHavePickaxe = false;

    //        isHaveItem = false;

    //        Transform itemTransform = itemSocket.transform.GetChild(0);

    //        GravityBody gb = this.GetComponent<GravityBody>();
    //        itemTransform.SetParent(gb.GetPlanet().transform);

    //        PlanetGravityForObjects pgfo = itemTransform.GetComponent<PlanetGravityForObjects>();
    //        pgfo.enabled = true;

    //        Rigidbody rb = itemTransform.GetComponent<Rigidbody>();
    //        rb.isKinematic = false;

    //        BoxCollider[] colliders = itemTransform.GetComponentsInChildren<BoxCollider>();
    //        foreach (var col in colliders)
    //        {
    //            col.enabled = true;
    //        }

    //        itemTransform.position = this.transform.position + this.transform.up * 3.5f;
    //        rb.AddForce((this.transform.up + this.transform.forward) * 200f);
    //    }
    //}
}
