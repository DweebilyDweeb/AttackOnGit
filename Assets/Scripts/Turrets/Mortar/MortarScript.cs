﻿using UnityEngine;
using System.Collections;

public class MortarScript : TurretScript
{

    private GameObject target;
    private float angle = 45;
    private float velxz;
    private float vely;
    private Vector3 direction;
    private float rotateSpd;
    private float velocity;
    private float explosion;
    private float height;

    public GameObject Bulletprefab;

    [SerializeField]
    private int[] towerCost;

    void Awake()
    {
        proximity = 10f;
    }
    // Use this for initialization
    protected override void Start()
    {
        //onGrid; 
        base.Start();
        minAttackDamage = 285;
        maxAttackDamage = 315;
        attackSpeed = 5.0f;
        rotateSpd = 1f;
        attackStyle = ATTSTYLE.NEAREST_FIRST;
        velocity = Mathf.Sqrt((proximity+0.1f) * 9.8f);
        explosion = 5.0f;
        height = 0.0f;
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        if (target)
        {
            // Gets Vector3 direction from traget
            direction = target.transform.position - transform.position;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction.normalized), Time.deltaTime * rotateSpd);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation, Time.deltaTime * rotateSpd);
        }
    }

    protected override Collider[] EnemiesInAttackRadius()
    {
        if (base.EnemiesInAttackRadius() == null)
            target = null;
        return base.EnemiesInAttackRadius();
    }

    protected override void Attacking(Collider[] enemies)
    {
        switch (attackStyle)
        {
            case ATTSTYLE.NEAREST_FIRST:
                {
                    float nearestDistance = proximity;
                    foreach (Collider enemy in enemies)
                    {
                        if ((transform.position - enemy.transform.position).magnitude < nearestDistance)
                        {
                            nearestDistance = new Vector3(enemy.transform.position.x - transform.position.x, 0, enemy.transform.position.z - transform.position.z).magnitude;
                            target = enemy.transform.gameObject;
                            height = enemy.transform.position.y - transform.position.y;
                        }
                    }
                    direction = target.transform.position - transform.position;
                    initialiseprojectile(nearestDistance, height);
                    break;
                }

            case ATTSTYLE.FURTHEST_FIRST:
                {
                    float longestDistance = 0f;
                    foreach (Collider enemy in enemies)
                    {
                        if ((transform.position - enemy.transform.position).magnitude >= longestDistance)
                        {
                            longestDistance = (enemy.transform.position - transform.position).magnitude;
                            target = enemy.transform.gameObject;
                            height = enemy.transform.position.y - transform.position.y;
                        }
                    }
                    direction = target.transform.position - transform.position;
                    initialiseprojectile(longestDistance, height);
                    break;
                }
        }        
    }

    public override void LevelUp()
    {
      
        towerLevel += 1;
        if (towerLevel == 2)
        {
            GameObject turretbase = Resources.Load("Turrets/Mortar/Base 1") as GameObject;
            transform.GetChild(0).GetComponent<MeshFilter>().mesh = turretbase.GetComponent<MeshFilter>().sharedMesh;
            GameObject turretbarrel = Resources.Load("Turrets/Mortar/Turret 1") as GameObject;
            transform.GetChild(0).GetChild(0).GetComponent<MeshFilter>().mesh = turretbarrel.GetComponent<MeshFilter>().sharedMesh;
        }
        else if (towerLevel == 3)
        {
            GameObject turretbase = Resources.Load("Turrets/Mortar/Base 2") as GameObject;
            transform.GetChild(0).GetComponent<MeshFilter>().mesh = turretbase.GetComponent<MeshFilter>().sharedMesh;
            GameObject turretbarrel = Resources.Load("Turrets/Mortar/Turret 2") as GameObject;
            transform.GetChild(0).GetChild(0).GetComponent<MeshFilter>().mesh = turretbarrel.GetComponent<MeshFilter>().sharedMesh;
        }
        LevelUpgrades(75, 100, 0.25f, 1.5f);
        explosion += 1.0f;
        velocity = Mathf.Sqrt((proximity + 0.1f) * 9.8f);

    }

    private void initialiseprojectile(float distance, float height)
    {

        GameObject projectile = Instantiate(Bulletprefab);
        projectile.GetComponent<MortarProjectileScript>().setDamage(minAttackDamage, maxAttackDamage);
        projectile.GetComponent<MortarProjectileScript>().setRadius(explosion);
        projectile.transform.position += transform.position;
        Vector3 xyzdirection;
        //sin^-1(distance * gravity)/velocity^2

        //angle =  Mathf.Asin((distance * 9.8f) / (velocity * velocity)) / 2 ;
        angle = Mathf.Atan((velocity * velocity + Mathf.Sqrt( (velocity * velocity * velocity * velocity) - 9.8f * ((9.8f * distance * distance) + (2 * height * velocity * velocity)))) / (9.8f * distance));

        velxz = Mathf.Cos(angle);
        vely = Mathf.Sin(angle);

        // XYZ proximity set
        xyzdirection.x = direction.normalized.x * velxz * velocity;
        xyzdirection.y = vely * velocity;
        xyzdirection.z = direction.normalized.z * velxz * velocity;

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb.velocity = xyzdirection;
    }

    public override int GetCost()
    {
        return towerCost[towerLevel];
    }
    public override int GetLevel()
    {
        return towerLevel;
    }
    public override int[] GetCostArray()
    {
        return towerCost;
    }
    public override float GetProximity()
    {
        return proximity;
    }
}
