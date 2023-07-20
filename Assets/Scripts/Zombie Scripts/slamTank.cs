using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;


public class slamTank : MonoBehaviour, IDamage
{
    [Header("Components")]
    [SerializeField] Renderer model;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Transform headPos;
    [SerializeField] Material material;
    [SerializeField] GameObject enemyUI;
    [SerializeField] Image hpBar;
    [Range(1, 10)][SerializeField] int hideHP;

    [Header("Tank Zombie Stats")]
    [SerializeField] int HP;
    [SerializeField] int damage;
    [SerializeField] int cooldown;

    [Header("Tank Zombie Navigation")]
    [Range(10, 360)][SerializeField] int viewAngle = 90;
    [Range(1, 8)][SerializeField] int playerFaceSpeed = 8;
    [SerializeField] int roamTimer = 3;
    [SerializeField] int roamDist = 10;

    Vector3 playerDir;
    Vector3 startingPos;
    Vector3 jumpPos;
    Vector3 velocity;
    int originalHP;
    bool playerInRange;
    bool destinationChosen;
    bool canSlam = true;
    bool isHitting;
    float angleToPlayer;
    float stoppingDistanceOrig;

    void Start()
    {
        originalHP = HP;
        gameManager.instance.updateGameGoal(1);
        stoppingDistanceOrig = agent.stoppingDistance;
        startingPos = transform.position;
        enemyUI.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && !canSeePlayer())
        {
            StartCoroutine(roam());
        }
        else if (agent.destination != gameManager.instance.player.transform.position)
            StartCoroutine(roam());
        
        enemyUI.transform.LookAt(gameManager.instance.player.transform.position);

    }

    IEnumerator roam()
    {
        if (agent.remainingDistance < 0.05f && !destinationChosen)
        {
            destinationChosen = true;
            agent.stoppingDistance = 0;
            yield return new WaitForSeconds(roamTimer);

            Vector3 randomPos = Random.insideUnitSphere * roamDist;
            randomPos += startingPos;

            NavMeshHit hit;
            NavMesh.SamplePosition(randomPos, out hit, roamDist, 1);
            agent.SetDestination(hit.position);

            destinationChosen = false;
        }
    }

    void facePlayer()
    {
        Quaternion rot = Quaternion.LookRotation(new Vector3(playerDir.x, 0, playerDir.z));
        transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime * playerFaceSpeed);
    }

    bool canSeePlayer()
    {
        agent.stoppingDistance = stoppingDistanceOrig;
        playerDir = gameManager.instance.player.transform.position - headPos.position;
        angleToPlayer = Vector3.Angle(new Vector3(playerDir.x, 0, playerDir.z), transform.forward);
        

        Debug.DrawRay(headPos.position, playerDir);

        RaycastHit hit;
        if (Physics.Raycast(headPos.position, playerDir, out hit))
        {
            if (hit.collider.CompareTag("Player") && angleToPlayer < viewAngle)
            {
                agent.SetDestination(gameManager.instance.player.transform.position);


                // If the agent reached their stopping distance
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    facePlayer();

                    // Deal damage to the player
                    if(!isHitting)
                    {
                        //StartCoroutine(dealDamage());
                    }
                }
                return true;
            }
        }
        agent.stoppingDistance = 0;
        return false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            StartCoroutine(PreExplode());

        }

    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    public void TakeDamage(int amount)
    {
        HP -= amount;
        agent.SetDestination(gameManager.instance.player.transform.position);
        StartCoroutine(flashDamage());
        updateUI();

        if (HP <= 0)
        {
            Destroy(gameObject);
            gameManager.instance.updateGameGoal(-1);
        }
    }

    public void updateUI()
    {
        enemyUI.SetActive(true);
        hpBar.fillAmount = (float)HP / originalHP;
        StartCoroutine(showHealth());
    }

    IEnumerator showHealth()
    {
        yield return new WaitForSeconds(hideHP);
        enemyUI.SetActive(false);
    }

    IEnumerator flashDamage()
    {
        model.material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        model.material = material;
    }

    IEnumerator dealDamage()
    {
        model.transform.localScale /= 1.5f;
        model.material = material;
        isHitting = true;
        gameManager.instance.player.GetComponent<playerController>().TakeDamage(damage);

        yield return new WaitForSeconds(1f);
        isHitting = false;

        StartCoroutine(Cooldown());
    }

    IEnumerator PreExplode()
    {
        model.material.color = Color.yellow;
        model.transform.localScale *= 1.5f;
        yield return new WaitForSeconds(cooldown);
        
        if (playerInRange)
        {
            StartCoroutine(dealDamage());
        }
        else
        {
            model.transform.localScale /= 1.5f;
            model.material = material;
        }
        
    }

    IEnumerator Cooldown()
    {
        canSlam = false;
        yield return new WaitForSeconds(cooldown);
        canSlam = true;
        if (playerInRange) 
        {
            StartCoroutine(PreExplode());        
        }
    }

}