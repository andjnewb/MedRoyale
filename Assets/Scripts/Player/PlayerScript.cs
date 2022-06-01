using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class PlayerScript : NetworkBehaviour
{
    //Floating text above player's heads.
    public TextMeshPro playerNameText;
    public GameObject floatingInfo;

    //player config
    public Vector2 sensitivity;

    [SyncVar]
    private int healthPoints;

    private Material playerMaterialClone;

    private Animator animator;
    private Rigidbody rigidbody;

    public ParticleSystem bloodEffect;

    //Mouse look
    private Vector2 rotation;

    //Scene stuff
    private SceneScript sceneScript;

    //Weapon stuff
    private int selectedWeaponLocal = 0;
    public GameObject[] weaponArray;

    [SyncVar(hook = nameof(OnWeaponChanged))]
    public int activeWeaponSynced = 0;


    [SyncVar(hook = nameof(OnNameChanged))]
    public string playerName;

    [SyncVar(hook = nameof(OnColorChanged))]
    public Color playerColor = Color.white;

    void OnNameChanged(string _old, string _new)
    {
        playerNameText.text = playerName;
    }
    void OnWeaponChanged(int _old, int _new)
    {
        //disable old weapon
        //in range and not null
        if (0 <= _old && _old < weaponArray.Length && weaponArray[_old] != null)
        {
            weaponArray[_old].SetActive(false);
        }

        if (0 <= _new && _new < weaponArray.Length && weaponArray[_new] != null)
        {
            weaponArray[_new].SetActive(true);
        }
    }
    void OnColorChanged(Color _old, Color _new)
    {
        playerNameText.color = _new;
        playerMaterialClone = new Material(GetComponent<Renderer>().material);
        playerMaterialClone.color = _new;
        GetComponent<Renderer>().material = playerMaterialClone;
    }

    private void Awake()
    {
        // disable all weapons
        foreach (var item in weaponArray)
            if (item != null)
                item.SetActive(false);

        sceneScript = GameObject.FindObjectOfType<SceneScript>();
        animator = GetComponent<Animator>();
        rigidbody = GetComponent<Rigidbody>();
        healthPoints = 100;
    }


    public override void OnStartLocalPlayer()
    {

        sceneScript.playerScript = this;

        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0, 1.67499995f, 0.2f); 

        floatingInfo.transform.localPosition = new Vector3(0, -0.3f, 0.6f);
        floatingInfo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        string name = "Player" + Random.Range(1, 100);
        Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        CmdSetupPlayer(name, color);
    }

    [Command]
    public void CmdChangeActiveWeapon(int newIndex)
    {
        activeWeaponSynced = newIndex;
    }

    [Command]
    public void CmdSetupPlayer(string _name, Color _col)
    {
        //player info sent to server, then server updates and syncs variables which handles it on all clients.
        playerName = _name;
        playerColor = _col;
        sceneScript.statusText = $"{playerName} joined.";
        healthPoints = 100;
    }
    [Command]
    public void CmdSendPlayerMessage()
    {
        if (sceneScript)
        {
            sceneScript.statusText = $"{playerName} says hello {Random.Range(10, 99)}";
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Keeps up from controlling remote players
        if (!isLocalPlayer) 
        {
            //Rotate the player name text towards you, if it isn't yours
            floatingInfo.transform.LookAt(Camera.main.transform);
            return; 
        }


        float moveX = Input.GetAxis("Horizontal") * Time.deltaTime * 4.0f;
        float moveZ = Input.GetAxis("Vertical") * Time.deltaTime * 4f;

        transform.Translate(moveX, 0, moveZ);

        updateRotation();

        switchWeapon();

        if (Input.GetButtonDown("Fire1"))
        {
            attack();
        }


        //Movement Animations
        animator.SetFloat("velocityX", Input.GetAxis("Horizontal"));
        animator.SetFloat("velocityZ", Input.GetAxis("Vertical"));
        animator.SetFloat("velocityY", rigidbody.velocity.y * 10);
        
        //Debug.Log(rigidbody.velocity.normalized.x + rigidbody.velocity.normalized.z * 100);

    }

    public void TakeDamage(int amount)
    {
        if (!isServer) return;

        healthPoints -= amount;

        //Only play the hit animation if the player is idling
        if (!animator.IsInTransition(0))
        {
            animator.SetTrigger("GotHit");
        }

        RpcDamage(1);
    }

    [ClientRpc]
    public void RpcDamage(int amount)
    {
        Debug.Log("Took damage:" + amount);
    }


    [Command]
    void attack()
    {

        animator.SetTrigger("AttackSword");
        Collider[] hitCollider = Physics.OverlapBox(weaponArray[selectedWeaponLocal].transform.position, weaponArray[selectedWeaponLocal].transform.localScale, Quaternion.identity);
        foreach (Collider col in hitCollider)
        {
            if (col != this.gameObject.GetComponent<Collider>() && col.tag == "Player")
            {
                
                col.GetComponent<PlayerScript>().TakeDamage(1);
                Debug.Log(playerName + "hit" + col.name + "for 1 damage, leaving them with " + col.GetComponent<PlayerScript>().healthPoints );

            }
                
        }
    }
    [Command]
    void switchWeapon()
    {
        if (Input.GetButtonDown("Fire2")) //Fire2 is mouse 2nd click and left alt
        {
            selectedWeaponLocal += 1;

            if (selectedWeaponLocal > weaponArray.Length)
                selectedWeaponLocal = 0;

            CmdChangeActiveWeapon(selectedWeaponLocal);
        }

    }
    [Command]
    void updateRotation()
    {
        //This functions handles rotation of the player and camera
        Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        Vector2 desiredVelocity = mouseInput * sensitivity;

        rotation += desiredVelocity * Time.deltaTime;

        rotation.y = Mathf.Clamp(rotation.y, -90, 90);


        transform.localEulerAngles = new Vector3(0, rotation.x, 0);
        Camera.main.transform.localEulerAngles = new Vector3(rotation.y, 0, 0);
    }

    //Upon collision with another GameObject, this GameObject will reverse direction
    private void OnTriggerEnter(Collider other)
    {
       
    }

    //Draw the Box Overlap as a gizmo to show where it currently is testing. Click the Gizmos button to see this
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        //Check that it is being run in Play Mode, so it doesn't try to draw this in Editor mode
        //Draw a cube where the OverlapBox is (positioned where your GameObject is as well as a size)
        Gizmos.DrawWireCube(weaponArray[selectedWeaponLocal].transform.position, weaponArray[selectedWeaponLocal].transform.localScale);
    }
}
