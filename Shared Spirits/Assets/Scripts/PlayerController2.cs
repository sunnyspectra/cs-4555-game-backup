using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController2 : MonoBehaviour
{

    Sprite sprite;
    public float speed2;                // Movement speed
    public Rigidbody rb2;              // Rigidbody for movement
    private Animator player2Animator;  // Animator for handling animations
    private Vector3 player2Scale;      // Original local scale for flipping
    private Vector3 movementVector2;    // Movement direction

    private Player2Controls player2Controls;  // Input system for Player 2


    //private Character character;

    private void Awake()
    {
        //character = GetComponent<Character>();
        player2Controls = new Player2Controls();
    }

    private void OnEnable()
    {
        player2Controls.Enable();
    }

    private void OnDisable()
    {
        player2Controls.Disable();
    }

    void Start()
    {
        rb2 = GetComponent<Rigidbody>();
        player2Scale = transform.localScale;
        player2Animator = GetComponent<Animator>();
    }

    public void HandleUpdate()
    {
        Vector2 input = player2Controls.Player2.Move2.ReadValue<Vector2>();
        
        // Set movement direction (normalized)
        movementVector2 = new Vector3(input.x, 0, input.y).normalized;

        // Set animation state based on movement
        player2Animator.SetBool("isRun", movementVector2.magnitude > 0);

        // Flip sprite based on movement direction
        if (input.x != 0)
        {
            float facingDirection = Mathf.Sign(input.x); // 1 for right, -1 for left
            transform.localScale = new Vector3(
                Mathf.Abs(player2Scale.x) * facingDirection, player2Scale.y, player2Scale.z);
        }

        //character.HandleUpdate();

        if (Input.GetKeyDown(KeyCode.N))
            StartCoroutine(Interact());
    }

    private void FixedUpdate()
    {
        // Move the player
        rb2.MovePosition(transform.position + movementVector2 * speed2 * Time.fixedDeltaTime);
    }



    IPlayerTriggerable currentlyInTrigger;
    private void OnMoveOver()
    {

    }

    IEnumerator Interact()
    {
        // Check for objects within a small radius
        Collider[] colliders = Physics.OverlapSphere(transform.position, 1.5f); // Adjust radius if necessary

        foreach (var collider in colliders)
        {
            // Try to get the Interactable component
            var interactable = collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                Debug.Log($"Interacting with {collider.gameObject.name}");
                yield return interactable.Interact(transform); // Trigger the interaction
                yield break; // Stop checking after finding one interactable
            }
        }

        Debug.LogWarning("No Interactable component found nearby.");
    }

    /*public object CaptureState()
    {

        return saveData;
    }*/

    public string Name
    {
        get => name;
    }

    public Sprite Sprite
    {
        get => sprite;
    }

    //public Character Character => character;
}

public class Player2SaveData
{
    public List<SpiritSaveData> spirits;
}