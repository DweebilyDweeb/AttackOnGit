﻿using UnityEngine;
using System.Collections;

public class SnowCrystalScript : TurretScript
{
    public GameObject Snow;

    private GameObject target;
    private GameObject curr;
    private GameObject particles;

    private Vector3 direction;

    private float Sloweffect;
    private float Snowradius;

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
        attackSpeed = 0.5f;
        direction = new Vector3(0, 0, 0);
        Sloweffect = 0.8f;
        Snowradius = 6.0f;
        timer = attackSpeed;
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        if (!curr && particles)
        {
            Destroy(particles);
        }
        if (curr)
        {
            direction = curr.transform.position - transform.position;
            if (particles)
            {
                particles.transform.position = curr.transform.position;
            }
            if (direction.magnitude >= proximity)
            {
                Destroy(curr.GetComponent<SnowStormScript>());
                Destroy(particles);
                curr = null;
                target = null;
                timer = attackSpeed;
            }
        }
    }

    protected override Collider[] EnemiesInAttackRadius()
    {
        if (base.EnemiesInAttackRadius() == null)
        {
            curr = null;
            target = null;
        }
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
                        if (!enemy.GetComponent<SnowStormScript>() || enemy.GetComponent<SnowStormScript>().Slow() < Sloweffect)
                        {
                            if ((transform.position - enemy.transform.position).magnitude < nearestDistance)
                            {
                                nearestDistance = (enemy.transform.position - transform.position).magnitude;
                                target = enemy.transform.gameObject;
                            }
                        }
                    }
                    if (!curr && target)
                    {
                        Slow(target);
                        curr = target;
                    }
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
                        }
                    }
                    break;
                }
        }
    }

    public override void LevelUp()
    {
        towerLevel += 1;
        if (towerLevel == 2)
        {
            GameObject turretbase = Resources.Load("Turrets/Snow/Base 1") as GameObject;
            transform.GetChild(0).GetComponent<MeshFilter>().mesh = turretbase.GetComponent<MeshFilter>().sharedMesh;
            GameObject turretcrystal = Resources.Load("Turrets/Snow/Crystal 1") as GameObject;
            transform.GetChild(0).GetChild(0).GetComponent<MeshFilter>().mesh = turretcrystal.GetComponent<MeshFilter>().sharedMesh;
        }
        else if (towerLevel == 3)
        {
            GameObject turretbase = Resources.Load("Turrets/Snow/Base 2") as GameObject;
            transform.GetChild(0).GetComponent<MeshFilter>().mesh = turretbase.GetComponent<MeshFilter>().sharedMesh;
            GameObject turretcrystal = Resources.Load("Turrets/Snow/Crystal 2") as GameObject;
            transform.GetChild(0).GetChild(0).GetComponent<MeshFilter>().mesh = turretcrystal.GetComponent<MeshFilter>().sharedMesh;
            transform.GetChild(0).GetChild(1).gameObject.SetActive(true);
        }

        LevelUpgrades(0.1f, 0.5f);
    }

    public void Slow(GameObject target)
    {
        if (!target.GetComponent<SnowStormScript>())
        {
            target.AddComponent<SnowStormScript>();
            target.GetComponent<SnowStormScript>().Snow(Snowradius, Sloweffect, true, target);

            particles = Instantiate(Snow);
            var x = Snow.GetComponent<ParticleSystem>().shape;
            x.radius = Snowradius;
        }
    }

    private void LevelUpgrades(float slow, float range)
    {
        Sloweffect -= slow;
        proximity += range;
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
