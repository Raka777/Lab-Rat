using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;


public class playerController : MonoBehaviour, IDamage
{
    [Header("----- Player -----")]
    [SerializeField] CharacterController controller;

    [Header("----- Player Stats -----")]
    [Range(1, 10)]public int HP;
    [Range(1, 10)][SerializeField] float playerSpeed;
    [Range(2, 5)] [SerializeField] float sprintDuration;
    [Range(1, 5)][SerializeField] int sprintCooldownLength;
    [Range(5,10)][SerializeField] float jumpHeight;
    [SerializeField] float gravity;
    [SerializeField] int jumpMax;

    [Header("----- Gun Stats -----")]
    public List<gunStats> gunList = new List<gunStats>();
    [SerializeField] GameObject gunModel;
    [SerializeField] GameObject muzzleFlash;
    [Range(0f, 2f)][SerializeField] float shootRate;
    [Range(0, 10)][SerializeField] int shootDamage;
    [Range(0, 100)][SerializeField] int shootDist;

    [Header("----- Flashlight -----")]
    [SerializeField] GameObject flashlightModel;
    [SerializeField] GameObject flashlight;
    [SerializeField] GameObject needBattery;
    [SerializeField] Light fLight;
    public bool drainOverTime;
    public float maxBrightness;
    public float minBrightness;
    public float drainRate;

    [Header("----- Class Objects -----")]
    public int originalHP;
    public int selectedGun;
    public bool hasFlashlight;
    Vector3 move;
    Vector3 velocity;
    int jumpCount;
    bool playerGrounded;
    bool isShooting;
    bool sprintCooldown;
    float playerSpeedOrig;

    void Start()
    {
        originalHP = HP;
        playerSpeedOrig = playerSpeed;
        fLight.enabled = false;

        spawnPlayer();

        if (gunList.Count > 0)
        {
            changeGunStats();
        }
    }

    void Update()
    {
        if (gameManager.instance.activeMenu == null)
        {
            Movement();
            useFlashlight();

            if (gunList.Count > 0)
            {
                scrollGuns();

                if (Input.GetButton("Shoot") && !isShooting)
                {
                    StartCoroutine(shoot());
                }
            }
        }
    }

    //movement function
    void Movement()
    {
        playerGrounded = controller.isGrounded;

        if (playerGrounded && velocity.y < 0)
        {
            velocity.y = 0f;
            jumpCount = 0;
        }

        move = (transform.right * Input.GetAxis("Horizontal")) +
                (transform.forward * Input.GetAxis("Vertical"));

        controller.Move(playerSpeed * Time.deltaTime * move);

        //make player jump
        if (Input.GetButtonDown("Jump") && jumpCount < jumpMax)
        {
            velocity.y = jumpHeight;
            ++jumpCount;
        }

        velocity.y -= gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (Input.GetButtonDown("Sprint"))
        {
            if (!sprintCooldown)
            {
                StartCoroutine(sprint());
            }
        }
    }

    IEnumerator shoot()
    {
        if (gunList[selectedGun].currAmmo > 0)
        {
            isShooting = true;

            StartCoroutine(flashMuzzle());
            --gunList[selectedGun].currAmmo;
            updatePlayerUI();

            RaycastHit hit;

            if (Physics.Raycast(Camera.main.ViewportPointToRay(new Vector2(0.5f, 0.5f)), out hit, shootDist))
            {
                IDamage damageable = hit.collider.GetComponent<IDamage>();

                if (damageable != null)
                {
                    damageable.TakeDamage(shootDamage);
                }
                else
                    Instantiate(gunList[selectedGun].hitEffect, hit.point, Quaternion.identity);
            }

            yield return new WaitForSeconds(shootRate);
            isShooting = false;
        }
        else
        {
            //if no ammo, display no ammo text
            StartCoroutine(noAmmoLeft());
        }
    }

    IEnumerator flashMuzzle()
    {
        muzzleFlash.SetActive(true);
        yield return new WaitForSeconds(.05f);
        muzzleFlash.SetActive(false);
    }

    IEnumerator noAmmoLeft()
    {
        gameManager.instance.noAmmo.SetActive(true);
        yield return new WaitForSeconds(2f);
        gameManager.instance.noAmmo.SetActive(false);
    }

    public void TakeDamage(int amount)
    {
        HP -= amount;
        StartCoroutine(gameManager.instance.playerFlashDamage());
        updatePlayerUI();

        if (HP <= 0)
        {
            gameManager.instance.youLose();
        }
    }

    public void useFlashlight()
    {
        fLight.intensity = Mathf.Clamp(fLight.intensity, minBrightness, maxBrightness);

        if (drainOverTime && fLight.enabled)
        {
            if (fLight.intensity > minBrightness)
            {
                fLight.intensity -= Time.deltaTime * (drainRate / 1000);
            }
        }

        if (hasFlashlight && Input.GetKeyDown(KeyCode.F))
        {
            if (fLight.intensity <= minBrightness)
            {
                StartCoroutine(recharge());
            }

            fLight.enabled = !fLight.enabled;
        }

        updatePlayerUI();
    }

    IEnumerator recharge()
    {
        needBattery.SetActive(true);
        yield return new WaitForSeconds(3f);
        needBattery.SetActive(false);
    }

    public void spawnPlayer()
    {
        controller.enabled = false;
        if (gameManager.instance.playerSpawnPos != null)
        {
            transform.position = gameManager.instance.playerSpawnPos.transform.position;
        }
        controller.enabled = true;
        HP = originalHP;
        updatePlayerUI();
    }

    IEnumerator sprint()
    {
        playerSpeed = playerSpeed * 1.5f;
        yield return new WaitForSeconds(sprintDuration);
        playerSpeed = playerSpeedOrig;
        StartCoroutine(sprintCooldownTimer());
    }

    IEnumerator sprintCooldownTimer()
    {
        sprintCooldown = true;
        yield return new WaitForSeconds(sprintCooldownLength);
        sprintCooldown = false;
    }

    public void updatePlayerUI()
    {
        gameManager.instance.playerHpBar.fillAmount = (float)HP / originalHP;

        if (hasFlashlight)
            gameManager.instance.batteryChargeBar.fillAmount = (float)fLight.intensity / maxBrightness;

        if (gunList.Count > 0)
        {
            gameManager.instance.currAmmoText.text = gunList[selectedGun].currAmmo.ToString("f0");
            gameManager.instance.maxAmmoText.text = gunList[selectedGun].maxAmmo.ToString("f0");
        }
    }

    public void gunPickup(gunStats gunStat)
    {
        //add gun to list, set gun stats, then update UI
        gunList.Add(gunStat);

        shootRate = gunStat.shootRate;
        shootDamage = gunStat.shootDamage;
        shootDist = gunStat.shootDist;

        gunModel.GetComponent<MeshFilter>().mesh = gunStat.gunModel.GetComponent<MeshFilter>().sharedMesh;
        gunModel.GetComponent<MeshRenderer>().material = gunStat.gunModel.GetComponent<MeshRenderer>().sharedMaterial;

        selectedGun = gunList.Count - 1;
        updatePlayerUI();
    }

    public void pickupFlashlight()
    {
        flashlightModel.GetComponent<MeshFilter>().mesh = flashlight.GetComponent<MeshFilter>().sharedMesh;
        flashlightModel.GetComponent<MeshRenderer>().material = flashlight.GetComponent<MeshRenderer>().sharedMaterial;
    }

    void scrollGuns()
    {
        //change gun depending on gun list
        if (Input.GetAxis("Mouse ScrollWheel") > 0 && selectedGun < gunList.Count - 1)
        {
            ++selectedGun;
            changeGunStats();
        }
        else if (Input.GetAxis("Mouse ScrollWheel") < 0 && selectedGun > 0)
        {
            --selectedGun;
            changeGunStats();
        }
    }

    void changeGunStats()
    {
        //set current gun stats to selected gun, the update UI
        shootDamage = gunList[selectedGun].shootDamage;
        shootDist = gunList[selectedGun].shootDist;
        shootRate = gunList[selectedGun].shootRate;

        gunModel.GetComponent<MeshFilter>().mesh = gunList[selectedGun].gunModel.GetComponent<MeshFilter>().sharedMesh;
        gunModel.GetComponent<MeshRenderer>().material = gunList[selectedGun].gunModel.GetComponent<MeshRenderer>().sharedMaterial;

        updatePlayerUI();
    }

    public void fillAmmo()
    {
        //set current ammo to max ammo, the update UI
        gunList[selectedGun].currAmmo = gunList[selectedGun].maxAmmo;
        updatePlayerUI();
    }

    public void replaceBattery(float amount)
    {
        fLight.intensity += amount;
    }
}