﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    private DoctorController owner;
    public int itemID = -1;
    public int initAmount = -1;
    private Animator animator;
    public LineRenderer rayTrailPrefab;
    public Transform shootPoint;
    public WEAPONTYPE weaponType;
    private List<ActiveTrail> activeTrails;
    public Projectile projectilePrefab;
    public float projectileLaunchForce=200;
    public AdvancedWeaponSettings advancedWeaponSettings;
    public int clipSize = 4;//弹夹里子弹数量(当前可以往里放的最大数量)
    public int clipContent;//当前弹夹里剩余子弹数
    private WEAPONSTATE currentWeaponState;
    private int fireNameHash;
    private int reloadNameHash;
    public float reloadTime = 2.0f;//更换弹夹的速度
    public float fireRete = 0.5f;//攻击频率（CD）
    private float shotTimer = -1;//CD计时器
    public AnimationClip fireAnimationClip;
    public AnimationClip reloadAnimationClip;
    public float damageValue = 1;
    public ParticleSystem raycastHitEffectPrefab;
    public WEAPONMODE weaponMode;
    public bool hasTelesopicView;
    private TelesopicView telesopicView;
    private float bulletSize;
    public ParticleSystem bulletViewEffect;
    private float currentDamageValue;
    public float decreaseSpeed;
    public AudioClip shootClip;
    public AudioClip hitWallClip;
    public AudioClip reloadClip;
    public AudioClip cockClip;
    private float chargeTimer;
    public AudioSource chargeAudio;
    private float stopChargeTimer;
    public AudioSource cockAudio;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    // Start is called before the first frame update
    void Start()
    {      
        if (rayTrailPrefab!=null)
        {
            //如果当前武器可以发射激光，那么需要先生成几个备用的预制体
            PoolManager.Instance.InitPool(rayTrailPrefab,8);
        }
        activeTrails = new List<ActiveTrail>();
        if (projectilePrefab!=null)
        {
            PoolManager.Instance.InitPool(projectilePrefab,8);
        }
        currentWeaponState = WEAPONSTATE.IDLE;
        fireNameHash= Animator.StringToHash("fire");
        reloadNameHash = Animator.StringToHash("reload");
        clipContent = clipSize;
        if (raycastHitEffectPrefab!=null)
        {
            PoolManager.Instance.InitPool(raycastHitEffectPrefab,8);
        }
        if (hasTelesopicView)
        {
            telesopicView = Camera.main.transform.GetComponent<TelesopicView>();
        }
        chargeTimer = 0;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateController();
        FireInput();
        if (Input.GetButtonDown("Reload"))
        {
            Reload();
        }
        if (shotTimer>0)
        {
            shotTimer -= Time.deltaTime;
        }
        UpdateTrailState();
    }
    /// <summary>
    /// 获取子弹数量
    /// </summary>
    /// <returns></returns>
    public int GetInitAmount()
    {
        return initAmount;
    }
    /// <summary>
    /// 获取武器的ID
    /// </summary>
    /// <returns></returns>
    public int GetID()
    {
        return itemID;
    }
    /// <summary>
    /// 选择当前武器
    /// </summary>
    public void Selected()
    {
        gameObject.SetActive(owner.GetAmmoAmount(itemID)!=0||clipContent!=0);
        animator.SetTrigger("selected");
        if (fireAnimationClip!=null)
        {
            animator.SetFloat("fireSpeed",fireAnimationClip.length/fireRete);
        }
        if (reloadAnimationClip!=null)
        {
            animator.SetFloat("reloadSpeed", reloadAnimationClip.length/reloadTime);
        }
        currentWeaponState = WEAPONSTATE.IDLE;
        owner.decreaseSpeed = decreaseSpeed;
        UIManager.Instance.ShowOrHideWeaponUIView(true);
        UIManager.Instance.ChangeWeaponUIView(itemID);
        UIManager.Instance.UpdateBulletNum(clipContent,owner.GetAmmoAmount(itemID));
        if (reloadClip != null)
        {
            AudioSourceManager.Instance.PlaySound(reloadClip);
        }
       
    }
    /// <summary>
    /// 收起武器
    /// </summary>
    public void PutAway()
    {
        gameObject.SetActive(false);
        if (weaponMode == WEAPONMODE.ACCUMULATION)
        {
            InitAccumulationWeapon();
        }
        if (weaponType == WEAPONTYPE.RAYCAST)
        {
            for (int i = 0; i < activeTrails.Count; i++)
            {
                activeTrails[i].renderer.gameObject.SetActive(false);
            }
            activeTrails.Clear();
        }
    }
    /// <summary>
    /// 捡起武器，制定当前武器拥有者即DoctorController的引用
    /// </summary>
    public void PickUp(DoctorController doctorController)
    {
         owner= doctorController;
    }
    /// <summary>
    /// 攻击方法
    /// </summary>
    private void Fire()
    {
        if (currentWeaponState!=WEAPONSTATE.IDLE||shotTimer>0)
        {
            return;
        }
        if (clipContent == 0)
        {
            if (!cockAudio.isPlaying)
            {
                cockAudio.Play();
            }
            return;
        }
        AudioSourceManager.Instance.PlaySound(shootClip);
        shotTimer = fireRete;
        clipContent -= 1;
        UIManager.Instance.UpdateBulletNum(clipContent, owner.GetAmmoAmount(itemID));
        animator.SetTrigger("fire");
        owner.cameraShaker.SetShakeValue(advancedWeaponSettings.shakeTime, 0.05f*advancedWeaponSettings.shakeStrength);
        currentWeaponState = WEAPONSTATE.FIRING;
        if (weaponType==WEAPONTYPE.RAYCAST)
        {
            RayCastShot();
        }
        else
        {
            ProjectileShot();
        }
    }
    /// <summary>
    /// 发射激光类型枪的攻击方式
    /// </summary>
    private void RayCastShot()
    {
        //发散比例（单位长度）
        float spreadRatio = advancedWeaponSettings.spreadAngle / Camera.main.fieldOfView;
        Vector2 spread =spreadRatio*Random.insideUnitCircle;
        Ray ray= Camera.main.ViewportPointToRay(Vector3.one*0.5f+ (Vector3)spread);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000, ~(1 << 8), QueryTriggerInteraction.Ignore))
        {
            ParticleSystem ps= PoolManager.Instance.GetInstance<ParticleSystem>(raycastHitEffectPrefab);
            ps.transform.position = hit.point;
            ps.transform.forward= hit.normal;
            ps.gameObject.SetActive(true);
            ps.Play();

            if (hit.collider.gameObject.layer==9)
            {
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                enemy.TakeDamage(damageValue);
            }
            else if (hit.collider.gameObject.layer==10)
            {
                Boss enemy = hit.collider.GetComponentInParent<Boss>();
                enemy.TakeDamage(damageValue,transform.position);
            }
            else
            {
                AudioSourceManager.Instance.PlaySound(hitWallClip);
            }

            if (rayTrailPrefab!=null)
            {
                LineRenderer lineRenderer= PoolManager.Instance.GetInstance<LineRenderer>(rayTrailPrefab);
                lineRenderer.gameObject.SetActive(true);
                Vector3[] trailPos = new Vector3[] { shootPoint.position,hit.point };
                lineRenderer.SetPositions(trailPos);
                activeTrails.Add(
                    new ActiveTrail()
                    {
                        renderer = lineRenderer,
                        direction=(trailPos[1]-trailPos[0]).normalized,
                        remainningTime=0.3f
                    }
                    ) ;
            }
        }
    }
    /// <summary>
    /// 更新激光拖尾效果（模拟位移）
    /// </summary>
    private void UpdateTrailState()
    {
        Vector3[] pos = new Vector3[2];
        for (int i = 0; i < activeTrails.Count; i++)
        {
            ActiveTrail activeTrail = activeTrails[i];
            activeTrail.renderer.GetPositions(pos);
            activeTrail.remainningTime -= Time.deltaTime;
            pos[0] += activeTrail.direction * 50 * Time.deltaTime;
            //pos[1] += activeTrail.direction * 50 * Time.deltaTime;
            activeTrail.renderer.SetPositions(pos);
            if (activeTrail.remainningTime<=0||Vector3.Distance(pos[0],pos[1])<=0.5f)
            {
                activeTrail.renderer.gameObject.SetActive(false);
                activeTrails.RemoveAt(i);
                i--;
            }
        }
    }
    /// <summary>
    /// 发射子弹类型枪的攻击方式(包括投掷类武器比如手雷)
    /// </summary>
    private void ProjectileShot()
    {
        Projectile projectile= PoolManager.Instance.GetInstance<Projectile>(projectilePrefab);
        projectile.gameObject.SetActive(true);
        if (weaponMode==WEAPONMODE.ACCUMULATION)
        {
            projectile.SetBulletDamageValue(currentDamageValue);
            projectile.SetBulletSize(bulletSize);
        }
        Vector2 angleDir = Random.insideUnitCircle*Mathf.Sin(advancedWeaponSettings.spreadAngle*Mathf.Deg2Rad);
        Vector3 dir = shootPoint.forward + (Vector3)angleDir;
        dir.Normalize();
        projectile.Launch(this,dir,projectileLaunchForce);
    }
    public Transform GetShootPoint()
    {
        return shootPoint;
    }
    /// <summary>
    /// 换弹夹
    /// </summary>
    public void Reload()
    {
        if (clipContent==clipSize||currentWeaponState!=WEAPONSTATE.IDLE)//弹夹子弹数已满
        {
            return;
        }
        int remainingBullet= owner.GetAmmoAmount(itemID);
        if (remainingBullet==0)
        {
            if (itemID==2||itemID==6)
            {
                PutAway();
            }
            return;
        }
        int chargeInClip= Mathf.Min(remainingBullet,clipSize-clipContent);
        clipContent += chargeInClip;
        currentWeaponState = WEAPONSTATE.RELOADING;
        owner.UpdateAmmoAmount(itemID,-chargeInClip);
        UIManager.Instance.UpdateBulletNum(clipContent, owner.GetAmmoAmount(itemID)); 
        animator.SetTrigger("reload");
        if (weaponMode==WEAPONMODE.ACCUMULATION)
        {
            bulletViewEffect.gameObject.SetActive(false);
            bulletViewEffect.startSize = 0;
        }
        if (reloadClip!=null)
        {
            AudioSourceManager.Instance.PlaySound(reloadClip);
        }
      
    }
    /// <summary>
    /// 更新武器状态控制器
    /// </summary>
    private void UpdateController()
    {
        animator.SetFloat("moveSpeed",owner.actualSpeed/5);
        AnimatorStateInfo animatorStateInfo= animator.GetCurrentAnimatorStateInfo(0);
        WEAPONSTATE newState;
        if (animatorStateInfo.shortNameHash==fireNameHash)
        {
            newState = WEAPONSTATE.FIRING;
        }
        else if (animatorStateInfo.shortNameHash==reloadNameHash)
        {
            newState = WEAPONSTATE.RELOADING;
        }
        else
        {
            newState = WEAPONSTATE.IDLE;
        }
        if (newState!=currentWeaponState)
        {
            WEAPONSTATE lastState= currentWeaponState;
            currentWeaponState = newState;
            if (lastState==WEAPONSTATE.FIRING&&clipContent==0)
            {
                Reload();
            }
        }
    }

    public bool HasBullet()
    {
        return clipContent > 0;
    }

    private void FireInput()
    {
        switch (weaponMode)
        {
            case WEAPONMODE.NORMAL:
                if (Input.GetMouseButtonDown(0))
                {
                    Fire();
                }
                if (hasTelesopicView)
                {
                    if (Input.GetMouseButton(1))
                    {
                        telesopicView.OpenTheTelesopicView();
                    }
                    else
                    {
                        telesopicView.OpenTheTelesopicView(false);
                    }
                    if (currentWeaponState == WEAPONSTATE.RELOADING)
                    {
                        telesopicView.OpenTheTelesopicView(false);
                    }
                }                
                break;
            case WEAPONMODE.AUTO:
                if (Input.GetMouseButton(0))
                {
                    Fire();
                }
                break;
            case WEAPONMODE.ACCUMULATION:
                if (Input.GetMouseButtonUp(0))
                {
                    Fire();
                    InitAccumulationWeapon();
                }
                else if (Input.GetMouseButton(0)&&clipContent>0&&
                    currentWeaponState!=WEAPONSTATE.RELOADING)
                {
                    AccumulateEnergy();
                }
                break;
            default:
                break;
        }
    }

    private void InitAccumulationWeapon()
    {
        chargeTimer = 0;
        chargeAudio.Stop();
        bulletViewEffect.gameObject.SetActive(false);
        bulletViewEffect.startSize = 0;
    }

    public void AccumulateEnergy()
    {
        bulletViewEffect.gameObject.SetActive(true);
        if (bulletViewEffect.startSize<=0.3f)
        {
            bulletViewEffect.startSize += Time.deltaTime;
        }
        if (currentDamageValue<=5*damageValue)
        {
            currentDamageValue += Time.deltaTime;
        }
        //第一次播放完整聚能音效
        if (chargeTimer<=0)
        {
            chargeTimer = Time.time;
            chargeAudio.time = 0;
            chargeAudio.Play();
        }
        //后续播放持续循环的部分（即聚能）
        if (Time.time-chargeTimer>=1.463f)
        {
            if (!chargeAudio.isPlaying)
            {
                //只持续播放某一个时间点到最后的音效
                //chargeAudio.time = 1.3f;
                chargeAudio.time = 0.4f;
                chargeAudio.Play();
                stopChargeTimer = Time.time;
            }
            if (Time.time-stopChargeTimer>=0.5f)
            {
                chargeAudio.Stop();
            }
        }
    }
}

public enum WEAPONTYPE
{ 
    RAYCAST,
    PROJECTILE
}
/// <summary>
/// 激光信息类
/// </summary>
public class ActiveTrail
{
    public LineRenderer renderer;
    public Vector3 direction;
    public float remainningTime;
}
/// <summary>
/// 武器的额外设置
/// </summary>
[System.Serializable]
public class AdvancedWeaponSettings
{
    public float spreadAngle;//偏移（发散）角度（单位不是传统意义上的度数，而是计量单位）
    public float shakeTime;//震动时间
    public float shakeStrength;//震动力量
}

public enum WEAPONSTATE
{ 
    IDLE,
    FIRING,
    RELOADING
}

public enum WEAPONMODE
{ 
    NORMAL,
    AUTO,
    ACCUMULATION
}
