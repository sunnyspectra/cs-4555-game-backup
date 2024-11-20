using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Player1Controls control1;
    private Player2Controls control2;

    private Sprite sprite;
    public float speed;
    public Rigidbody rb;
    private Animator anim;
    private Vector3 localScale;
    private Vector3 moveDirection;

    private int playerId;

    private void Awake()
    {
        control1 = new Player1Controls();
        control2 = new Player2Controls();
    }

    private void OnEnable()
    {
        control1.Enable();
        control2.Enable();
    }

    private void OnDisable()
    {
        control1.Disable();
        control2.Disable();
    }

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        localScale = transform.localScale;
        anim = GetComponent<Animator>();
    }

    public void SetPlayer(int id)
    {
        playerId = id;
    }

    public void HandleUpdate()
    {
        Vector2 input = Vector2.zero;

        if (playerId == 1)
        {
            input = control1.Player.Move.ReadValue<Vector2>();
        }
        else if (playerId == 2)
        {
            input = control2.Player2.Move2.ReadValue<Vector2>();
        }

        moveDirection = new Vector3(input.x, 0, input.y).normalized;
        anim.SetBool("isRun", moveDirection.magnitude > 0);

        if (input.x != 0)
        {
            float facingDirection = Mathf.Sign(input.x);
            transform.localScale = new Vector3(
                Mathf.Abs(localScale.x) * facingDirection, localScale.y, localScale.z);
        }

        if (playerId == 1 && Input.GetKeyDown(KeyCode.Z))
            StartCoroutine(Interact());
        else if (playerId == 2 && Input.GetKeyDown(KeyCode.N))
            StartCoroutine(Interact());
    }

    private void FixedUpdate()
    {
        rb.MovePosition(transform.position + moveDirection * speed * Time.fixedDeltaTime);
    }

    IEnumerator Interact()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 1.5f);

        foreach (var collider in colliders)
        {
            var interactable = collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                Debug.Log($"Interacting with {collider.gameObject.name}");
                yield return interactable.Interact(transform);
                yield break; 
            }
        }

        Debug.LogWarning("No Interactable component found nearby.");
    }

    public string Name
    {
        get => name;
    }

    public Sprite Sprite
    {
        get => sprite;
    }
}

public class PlayerSaveData
{
    public List<SpiritSaveData> spirits;
}

public class Player2SaveData
{
    public List<SpiritSaveData> spirits;
}
