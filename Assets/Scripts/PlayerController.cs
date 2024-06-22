﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dojo;
using Dojo.Starknet;
using dojo_bindings;
using System.Threading.Tasks;

public class PlayerController : MonoBehaviour
{

    public Rigidbody2D theRB;
    public float moveSpeed;

    public Animator myAnim;

    public static PlayerController instance;

    public string areaTransitionName;
    private Vector3 bottomLeftLimit;
    private Vector3 topRightLimit;

    FixedJoystick joystick;

    public bool canMove = true;
    // Use this for initialization
    [SerializeField] WorldManager worldManager;

    [SerializeField] WorldManagerData dojoConfig;

    [SerializeField] GameManagerData gameManagerData;

    public BurnerManager burnerManager;
    private Dictionary<FieldElement, string> spawnedAccounts = new();
    [SerializeField] Actions actions;

    public JsonRpcClient provider;
    public Account masterAccount;

    private bool isMoving = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        DontDestroyOnLoad(gameObject);
    }


    void Start()
    {
        provider = new JsonRpcClient(dojoConfig.rpcUrl);
        masterAccount = new Account(provider, new SigningKey(gameManagerData.masterPrivateKey), new FieldElement(gameManagerData.masterAddress));
        burnerManager = new BurnerManager(provider, masterAccount);

        joystick = GameObject.FindGameObjectWithTag("Joystick").GetComponent<FixedJoystick>();

        #if UNITY_STANDALONE_WIN
            joystick.gameObject.SetActive(false);
        #endif
    }

    // Update is called once per frame
    async void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        #if UNITY_ANDROID || UNITY_IOS
        
            horizontal = joystick.Horizontal;
            vertical = joystick.Vertical;

        #endif
        //float horizontal = joystick.Horizontal;
        //float vertical = joystick.Vertical;

        if (canMove)
        {
            theRB.velocity = new Vector2(horizontal, vertical) * moveSpeed;

        }
        else
        {
            theRB.velocity = Vector2.zero;
        }

        myAnim.SetFloat("moveX", theRB.velocity.x);
        myAnim.SetFloat("moveY", theRB.velocity.y);

        if (horizontal == 1 || horizontal == -1 || vertical == 1 || vertical == -1)
        {
            if (canMove && !isMoving)
            {
                myAnim.SetFloat("lastMoveX", horizontal);
                myAnim.SetFloat("lastMoveY", vertical);

                // Check for movement in each direction
                if (vertical > 0 && horizontal == 0)
                {
                    Debug.Log("Moving up");
                    Task taskUp = actions.move(burnerManager.CurrentBurner ?? masterAccount, new Direction.Up());
                    StartCoroutine(DoMove(taskUp));
                    // await Move(new Direction.Up());
                }
                else if (vertical < 0 && horizontal == 0)
                {   Debug.Log("Moving down");
                    Task taskDown = actions.move(burnerManager.CurrentBurner ?? masterAccount, new Direction.Down());
                    StartCoroutine(DoMove(taskDown));
                }
                if (horizontal > 0 && vertical == 0)
                {
                    Debug.Log("Moving right");
                    Task taskRight = actions.move(burnerManager.CurrentBurner ?? masterAccount, new Direction.Right());
                    StartCoroutine(DoMove(taskRight));
                    // await Move(new Direction.Right());
                }
                else if (horizontal < 0 && vertical == 0)
                {
                    Debug.Log("Moving left");
                    Task taskLeft = actions.move(burnerManager.CurrentBurner ?? masterAccount, new Direction.Left());
                    StartCoroutine(DoMove(taskLeft));
                    // await Move(new Direction.Left());
                }
            }
        }

        transform.position = new Vector3(Mathf.Clamp(transform.position.x, bottomLeftLimit.x, topRightLimit.x), Mathf.Clamp(transform.position.y, bottomLeftLimit.y, topRightLimit.y), transform.position.z);
    }

    public void SetBounds(Vector3 botLeft, Vector3 topRight)
    {
        bottomLeftLimit = botLeft + new Vector3(.5f, 1f, 0f);
        topRightLimit = topRight + new Vector3(-.5f, -1f, 0f);
    }

    private IEnumerator DoMove(Task task)
        {
            isMoving = true;
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("Move completed");
            }
            isMoving = false;
        }
    public void ActivateJoystick(bool val)
    {
        #if UNITY_ANDROID || UNITY_IOS 
            joystick.gameObject.SetActive(val);
        #endif
    }
}