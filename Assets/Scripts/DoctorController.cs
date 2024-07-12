using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DoctorController : MonoBehaviour
{
    private CharacterController characterController;
    public float moveSpeed;
    public float mouseSensitivity;//角度值
    private float angleY;
    private float angleX;
    private Transform cameraTrans;
    public float runningSpeed;
    private bool isGrounded;
    public float jumpSpeed;
    private CollisionFlags collisionFlags;
    public int currentWeaponID;
    private Dictionary<int,Weapon> weaponsDict;
    private Transform weaponPlaceTrans;
    private Dictionary<int, int> ammoInventory;//玩家的武器库（背包，当前玩家某个武器以及其剩余的子弹数量）
    public int currentHP;
    public int initHP;
    public float decreaseSpeed;
    public float actualSpeed;
    public CameraShaker cameraShaker;
    public bool dead;
    public Transform deadPostionTrans;
    public AudioClip jumpClip;
    public AudioClip landClip;
    private bool canPlayLandClip;
    public AudioClip deadClip;
    public AudioClip hurtClip;
    public Weapon[] weapons;
    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        moveSpeed = 5;
        mouseSensitivity = 2.4f;
        angleY = transform.eulerAngles.y;
        cameraTrans = Camera.main.transform;
        angleX = cameraTrans.eulerAngles.x;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        runningSpeed = 10;
        jumpSpeed = 0;
        isGrounded = true;
        currentWeaponID = -1;
        weaponPlaceTrans = cameraTrans.Find("WeaponPlace");
        //weapons = new List<Weapon>()
        //{
        //    weaponPlaceTrans.GetChild(0).GetComponent<Weapon>(),
        //    weaponPlaceTrans.GetChild(1).GetComponent<Weapon>(),
        //    weaponPlaceTrans.GetChild(2).GetComponent<Weapon>(),
        //};
        weaponsDict = new Dictionary<int, Weapon>();
        ammoInventory = new Dictionary<int, int>();
        currentHP = initHP;
        UIManager.Instance.ShowOrHideWeaponUIView(false);
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].gameObject.SetActive(false);
            weapons[i].PickUp(this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (dead)
        {
            return;
        }
        Move();
        TurnAndLook();
        Jump();
        ChangeCurrentWeapon();
    }
    /// <summary>
    /// 移动
    /// </summary>
    private void Move()
    {
        actualSpeed= Input.GetButton("Run") ? runningSpeed-decreaseSpeed : moveSpeed-decreaseSpeed;
        Vector3 move = Vector3.zero;
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        move = new Vector3(h, 0, v);
        move.Normalize();
        move = move * actualSpeed * Time.deltaTime;
        move = transform.TransformDirection(move);
        collisionFlags= characterController.Move(move);
        if (h<=0.1f&&v<=0.1f)
        {
            actualSpeed = 0;
        }
    }
    /// <summary>
    /// 转向看
    /// </summary>
    private void TurnAndLook()
    {
        float turnAngle = Input.GetAxis("Mouse X")*mouseSensitivity;
        angleY = angleY + turnAngle;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x,angleY,transform.eulerAngles.z);
        float lookAngle= -Input.GetAxis("Mouse Y") * mouseSensitivity;
        angleX= Mathf.Clamp(angleX + lookAngle,-90f,90f);
        cameraTrans.eulerAngles = new Vector3(angleX,cameraTrans.eulerAngles.y,cameraTrans.eulerAngles.z);
    }
    /// <summary>
    /// 跳跃
    /// </summary>
    private void Jump()
    {
        if (Input.GetButton("Jump")&&isGrounded)
        {
            isGrounded = false;
            jumpSpeed = 5;
            AudioSourceManager.Instance.PlaySound(jumpClip,0.8f,1.1f);
            canPlayLandClip = true;
        }
        if (!isGrounded)//跳起来了，不在地面上
        {
            jumpSpeed= jumpSpeed - 10 * Time.deltaTime;
            Vector3 jump = new Vector3(0,jumpSpeed*Time.deltaTime,0);
            collisionFlags= characterController.Move(jump);
            if (collisionFlags==CollisionFlags.Below)
            {
                jumpSpeed = 0;
                isGrounded = true;
            }
        }
        if (isGrounded&&collisionFlags==CollisionFlags.None)
        {
            if (canPlayLandClip)
            {
                canPlayLandClip = false;
                AudioSourceManager.Instance.PlaySound(landClip, 0.8f, 1.1f);
            }
            isGrounded = false;
        }
    }
    /// <summary>
    /// 具体切换武器
    /// </summary>
    /// <param name="id"></param>
    private void ChangeWeapon(int id)
    {
        if (weaponsDict.Count == 0)
        {
            return; 
        }
        ////处理索引的上下边界
        //if (id >= weaponsDict.Count)
        //{
        //    id = 0;
        //}
        //else if (id <= -1)
        //{
        //    id = weaponsDict.Count - 1;
        //}
        if (id>weaponsDict.Keys.Max())
        {
            id = weaponsDict.Keys.Min();
        }
        else if(id<weaponsDict.Keys.Min())
        {
            id = weaponsDict.Keys.Max();
        }
        if (id == currentWeaponID)//只有一种武器时不切换,否则会出现颠簸颤抖的情况
        {
            return;
        }
        while (!weaponsDict.ContainsKey(id))
        {
            if (id>currentWeaponID)
            {
                id++;
            }
            else
            {
                id--;
            }
        }
        //隐藏上一把武器
        if (currentWeaponID!=-1)//排除第一次没有武器的情况
        {
            weaponsDict[currentWeaponID].PutAway();
        }
        //显示当前武器
        weaponsDict[id].Selected();
        currentWeaponID = id;
    }
    /// <summary>
    /// 切换当前武器
    /// </summary>
    public void ChangeCurrentWeapon(bool autoChange=false)
    {
        if (autoChange)
        {
            //切换到最新拿到的一把
            //ChangeWeapon(weaponsDict.Count-1);
            ChangeWeapon(weaponsDict.Keys.Last());
        }
        else
        {
            if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                ChangeWeapon(currentWeaponID + 1);
            }
            else if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                ChangeWeapon(currentWeaponID - 1);
            }
            for (int i = 0; i < 10; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    int num;
                    if (i == 0)
                    {
                        num = 10;
                    }
                    else
                    {
                        num = i - 1;
                    }
                    if (weaponsDict.ContainsKey(num))
                    {
                        ChangeWeapon(num);
                    }
                }
            }

        }
        
    }
    /// <summary>
    /// 拾取武器
    /// </summary>
    /// <param name="weaponName"></param>
    public void PickUpWeapon(int weaponID)
    {
        if (weaponsDict.ContainsKey(weaponID))
        {
            //补充弹药
            Weapon weapon = weaponsDict[weaponID];
            ammoInventory[weapon.GetID()] = weapon.GetInitAmount();
            weapon.clipContent = weapon.clipSize;
            if (currentWeaponID==weaponID)
            {
                UIManager.Instance.UpdateBulletNum(weapon.clipSize, weapon.GetInitAmount());
            }
        }
        else//当前这种名称的武器列表里没有
        {
            //GameObject weaponGo= Instantiate(Resources.Load<GameObject>("Prefabs/Weapons/"+weaponID.ToString()));
            //weaponGo.transform.SetParent(weaponPlaceTrans);
            //weaponGo.transform.localPosition = Vector3.zero;
            //weaponGo.transform.localRotation = Quaternion.identity;
            //weaponGo.gameObject.SetActive(false);
            //Weapon weapon = weaponGo.GetComponent<Weapon>();
            //weapon.PickUp(this);
            weapons[weaponID].clipContent = weapons[weaponID].clipSize;
            weaponsDict.Add(weaponID,weapons[weaponID]);
            ammoInventory.Add(weaponID, weapons[weaponID].GetInitAmount());
            ChangeWeapon(weaponID);
        }
    }
    /// <summary>
    /// 获取某一种武器在武器库中子弹的剩余数量
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public int GetAmmoAmount(int id)
    {
        int value = 0;
        ammoInventory.TryGetValue(id,out value);
        return value;
    }
    /// <summary>
    /// 更新当前武器库中某一种武器装备剩余子弹的数量
    /// </summary>
    /// <param name="id"></param>
    /// <param name="value"></param>
    public void UpdateAmmoAmount(int id,int value)
    {
        if (ammoInventory.ContainsKey(id))
        {
            ammoInventory[id] += value;
        }
    }
    public void TakeDamage(int value)
    {
        if (dead)
        {
            return;
        }      
        
        if (value<0)
        {
            if (currentHP<initHP)
            {
                currentHP -= value;
                if (currentHP>= initHP)
                {
                    currentHP = initHP;
                }
            }
        }
        else
        {
            currentHP -= value;
        }
   
        if (currentHP <= 0)
        {
            dead = true;
            cameraTrans.localPosition = deadPostionTrans.localPosition;
            cameraTrans.eulerAngles = deadPostionTrans.eulerAngles;
            weaponPlaceTrans.gameObject.SetActive(false);
            currentHP = 0;
            UIManager.Instance.ShowDeadUI();
            AudioSourceManager.Instance.PlaySound(deadClip);
        }
        else
        {
            if (value>0)
            {
                AudioSourceManager.Instance.PlaySound(hurtClip);
                UIManager.Instance.ShowTakeDamageView();
                cameraShaker.SetShakeValue(0.2f, 0.5f);
            }
            
        }
        UIManager.Instance.UpdateHPValue(currentHP);
    }
}
