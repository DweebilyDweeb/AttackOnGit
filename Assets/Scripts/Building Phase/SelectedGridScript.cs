﻿
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SelectedGridScript : MonoBehaviour
{

    // Check if the placing logic is correct
    private bool isAbleToPlacePrefab;
    // Check if there are enough resources to build
    private bool hasResources;

    // The renderer for the grid
    private Renderer theGridRenderer;
    // The selected and the actual grid in the gridSystem
    private Grid selectedGrid;

    // Where should the selected grid be at when first instantiated
    public int startSelectedGridID;

    // The grid system
    public GridSystem theGridSystem;
    // The building phase 
    public BuildingPhaseSystemScript buildingPhaseSystem;

    // Where the spawners are to check for ai blocking
    public GameObject[] spawners;
    // The selection the player chose
    public GameObject selectedPrefab;

    // Not CSGO. Just the instantiated version of the selectedPrefab, to show where the turret/tower is
    private GameObject showcaseGO;

    public GameObject rangeIndicator;
    private GameObject rangeSphere;

    public enum PHASE_MODE
    {
        LIST_OPEN = 0,
        LIST_CLOSE
    } public PHASE_MODE phaseMode;

    // Testing
    public Camera mainCamera;
    public GameObject explosionPrefab;
    // Use this for initialization
    void Start()
    {
        isAbleToPlacePrefab = true;
        hasResources = true;
        phaseMode = PHASE_MODE.LIST_OPEN;
        theGridRenderer = GetComponent<Renderer>();

        if (theGridSystem == null)
        {
            Debug.Log("No grid system found");
            return;
        }
        if (theGridSystem.GetGrid(startSelectedGridID) == null)
        {
            Debug.Log("Not valid grid");
            return;
        }
        selectedGrid = theGridSystem.GetGrid(startSelectedGridID).GetComponent<Grid>();
        if (selectedGrid == null)
            print("selecting grid not initialised");
        transform.position = theGridSystem.GetGrid(startSelectedGridID).transform.position;
        theGridSystem.RenderGrids(true);
    }

    void ListOpenUpdate()
    {
        RaycastHit hit;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
       
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.GetComponent<Grid>() != null)
            {
                if (selectedGrid != hit.transform.GetComponent<Grid>())
                {
                    selectedGrid = hit.transform.GetComponent<Grid>();
                    transform.position = theGridSystem.GetGrid(selectedGrid.GetID()).transform.position + new Vector3(0, -0.05f, 0);
                    if (selectedPrefab != null)
                    {
                        ChangeTurretTranslateOnTower();
                        CanGameobjectBePlaced();
                    }
                }
                if (Input.GetMouseButtonUp(0))
                {
                    if (EventSystem.current.IsPointerOverGameObject())
                    {
                        return;
                    }
                    if (isAbleToPlacePrefab && hasResources)
                        PlaceGameObject();
                }
            }
        }
        // If there is no selection
        if (selectedPrefab == null)
        {
            // Change color to default white
            theGridRenderer.material.SetColor("_Color", Color.white);
        }
        // If it is able to place the prefab
        else if (isAbleToPlacePrefab && hasResources)
        {
            // Change color to peaceful green
            theGridRenderer.material.SetColor("_Color", Color.green);
        }
        // If it is unable to place the prefab
        else if (!isAbleToPlacePrefab || !hasResources)
        {
            // Change color to bloody red
            theGridRenderer.material.SetColor("_Color", Color.red);
        }
    }
    void ListCloseUpdate()
    {
        if (!buildingPhaseSystem.nextWaveButton.IsActive())
            buildingPhaseSystem.nextWaveButton.gameObject.SetActive(true);
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
                if (hit.transform.GetComponent<Grid>() != null)
                {
                    if (selectedGrid != hit.transform.GetComponent<Grid>())
                    {
                        selectedGrid = hit.transform.GetComponent<Grid>();
                        transform.position = theGridSystem.GetGrid(selectedGrid.GetID()).transform.position + new Vector3(0, -0.05f, 0);

                        if (selectedGrid.wall == null && selectedGrid.tower == null)
                        {
                            theGridRenderer.material.SetColor("_Color", Color.white);
                            buildingPhaseSystem.SelectedNothingButton();
                            if (rangeSphere)
                                Destroy(rangeSphere);
                        }
                        else if (selectedGrid.tower != null)
                        {
                            theGridRenderer.material.SetColor("_Color", Color.green);
                            buildingPhaseSystem.SelectedTowerButton();
                            UpdateRangeSphere();
                            UpdateUpgradeCost();
                        }
                        else if (selectedGrid.wall != null)
                        {
                            theGridRenderer.material.SetColor("_Color", Color.green);
                            buildingPhaseSystem.SelectedWallButton();
                        }
                    }
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        switch (phaseMode)
        {
            case PHASE_MODE.LIST_OPEN:
                //KeyboardControls();
                ListOpenUpdate();
                break;


            case PHASE_MODE.LIST_CLOSE:
                ListCloseUpdate();
                break;
        }
        if(Input.GetKey(KeyCode.L))
        {
            print(selectedGrid.tower.GetComponentInChildren<TurretScript>().Level);
        }
    }
    // Check functions
    private void CheckWallsCanBuild()
    {
        if (selectedGrid.wall != null)
        {
            isAbleToPlacePrefab = false;
            return;
        }
        selectedGrid.wall = new GameObject();
        for (int i = 0; i < spawners.Length; ++i)
        {
            bool hasPath = spawners[i].GetComponent<MonsterSpawner>().GeneratePath();
            if (hasPath == false)
            {
                isAbleToPlacePrefab = false;
                break;
            }
            else
            {
                isAbleToPlacePrefab = true;
            }
        }
        DestroyImmediate(selectedGrid.wall);
    }
    private void CheckTurretsCanBuild()
    {
        isAbleToPlacePrefab = (selectedGrid.wall != null && selectedGrid.tower == null);
    }

    // Call the above check functions according to the selection and changing the selection grid color accordingly
    private void CanGameobjectBePlaced()
    {
        if (selectedPrefab.CompareTag("Wall"))
        {
            if (rangeSphere)
                Destroy(rangeSphere);
            CheckWallsCanBuild();
        }
        else if (selectedPrefab.CompareTag("Turret"))
        {
            CheckTurretsCanBuild();
        }
        else
            return;
    }
    // Called to place the gameobject
    public void PlaceGameObject()
    {
        // If we are selecting a turret
        if (selectedPrefab.CompareTag("Turret"))
        {
            // If there's a wall and there are no towers
            if (selectedGrid.wall != null && selectedGrid.tower == null)
            {
                selectedGrid.tower = GameObject.Instantiate(selectedPrefab);
                selectedGrid.tower.transform.SetParent(selectedGrid.wall.transform);
                selectedGrid.tower.transform.position = selectedGrid.wall.transform.position;

                CapsuleCollider wallCollider = selectedGrid.wall.GetComponent<CapsuleCollider>();
                selectedGrid.tower.transform.position = new Vector3(selectedGrid.wall.transform.position.x, (wallCollider.height + wallCollider.center.y) / 2, selectedGrid.wall.transform.position.z);
                buildingPhaseSystem.amountToBuildTowers -= selectedGrid.tower.GetComponentInChildren<TurretScript>().GetCost();
            }
        }
        // If we are selecting a wall
        else if (selectedPrefab.CompareTag("Wall"))
        {
            // If there's no wall
            if (selectedGrid.wall == null)
            {
                selectedGrid.wall = GameObject.Instantiate(selectedPrefab);
                selectedGrid.wall.AddComponent<TurretTowerScript>();
                selectedGrid.wall.GetComponent<TurretTowerScript>().tileID = selectedGrid.GetID();
                selectedGrid.wall.GetComponent<TurretTowerScript>().gridSystem = theGridSystem;
                selectedGrid.wall.GetComponent<TurretTowerScript>().explosion = explosionPrefab;

                selectedGrid.wall.AddComponent<Health>();
                selectedGrid.wall.GetComponent<Health>().SetMaxHealth(100);
                selectedGrid.wall.GetComponent<Health>().SetCurrentHealth(100);
                selectedGrid.wall.GetComponent<HealthBar>().transform.SetParent(selectedGrid.wall.transform);
                selectedGrid.wall.GetComponent<HealthBar>().enabled = true;
                selectedGrid.wall.transform.SetParent(selectedGrid.transform);
                buildingPhaseSystem.numberOfBuildableWalls -= 1;
            }
            else
            {
                isAbleToPlacePrefab = false;
            }
        }
        CheckCostAndNumber();
        CanGameobjectBePlaced();
        buildingPhaseSystem.UpdateText();
    }

    // Called when the selection of the prefab changes
    public void ChangeSelected()
    {
        if (showcaseGO)
            Destroy(showcaseGO);
        if (selectedPrefab == null)
            return;
        showcaseGO = GameObject.Instantiate(selectedPrefab);
        showcaseGO.transform.SetParent(transform);
        showcaseGO.transform.position = transform.position;
        if (selectedGrid == null)
            selectedGrid = theGridSystem.GetGrid(startSelectedGridID).GetComponent<Grid>();
        CanGameobjectBePlaced();
        ChangeTurretTranslateOnTower();
        CheckCostAndNumber();
        CheckTowerUpdate();
    }
    // A function for the turrets, to put the selection of the prefab *above* the wall to make it look realistic
    private void ChangeTurretTranslateOnTower()
    {
        if (!selectedPrefab.CompareTag("Turret"))
            return;
        if (selectedGrid.wall != null)
        {
            CapsuleCollider wallCollider = selectedGrid.wall.GetComponent<CapsuleCollider>();
            if (wallCollider == null)
                return;
            showcaseGO.transform.position = new Vector3(showcaseGO.transform.position.x, (wallCollider.height + wallCollider.center.y) / 2, showcaseGO.transform.position.z);
         }
        else
        {
            showcaseGO.transform.position = selectedGrid.transform.position;
        }
    }

    // A function to check if there is still enough currency and walls to keep building, else turn the grid red
    private void CheckCostAndNumber()
    {
        if (selectedPrefab.CompareTag("Turret"))
        {
            if (buildingPhaseSystem.amountToBuildTowers < selectedPrefab.GetComponentInChildren<TurretScript>().GetCost())
            {
                hasResources = false;
            }
            else
            {
                hasResources = true;
            }
        }
        else if (selectedPrefab.CompareTag("Wall"))
        {
            if (buildingPhaseSystem.numberOfBuildableWalls <= 0)
            {
                hasResources = false;
            }
            else
            {
                hasResources = true;
            }
        }

    }
    public void CheckTowerUpdate()
    {
        if (selectedGrid.tower == null)
        {
            buildingPhaseSystem.upgradeButton.gameObject.SetActive(false);
            return;
        }
        else if (selectedGrid.tower.GetComponentInChildren<TurretScript>().GetLevel() >= selectedGrid.tower.GetComponentInChildren<TurretScript>().MaxLevel ||
            buildingPhaseSystem.amountToBuildTowers < selectedGrid.tower.GetComponentInChildren<TurretScript>().GetCost())
        {
            buildingPhaseSystem.upgradeButton.gameObject.SetActive(false);
        }
        else if (phaseMode == PHASE_MODE.LIST_CLOSE)
        {
            buildingPhaseSystem.upgradeButton.gameObject.SetActive(true);
        }
    }

    // Functions for other scripts to access our variables and functions
    public void ChangeToClosePhase()
    {
        if (showcaseGO)
        {
            Destroy(showcaseGO);
        }
        if (selectedGrid.wall == null && selectedGrid.tower == null)
        {
            theGridRenderer.material.SetColor("_Color", Color.white);
            buildingPhaseSystem.SelectedNothingButton();
        }
        else if (selectedGrid.tower != null)
        {
            theGridRenderer.material.SetColor("_Color", Color.green);
            buildingPhaseSystem.SelectedTowerButton();
        }
        else if (selectedGrid.wall != null)
        {
            theGridRenderer.material.SetColor("_Color", Color.green);
            buildingPhaseSystem.SelectedWallButton();
        }
        for (int i = 0; i < spawners.Length; ++i)
        {
            spawners[i].GetComponent<MonsterSpawner>().GeneratePath();
        }
        UpdateRangeSphere();
        UpdateUpgradeCost();
    }
    public void ChangeToOpenPhase()
    {
        if (rangeSphere)
            Destroy(rangeSphere);
        ChangeSelected();
        buildingPhaseSystem.DisableAllButtons();
        UpdateRangeSphere();
    }
    public int DestroySelectedTurret()
    {
        if (selectedGrid.tower == null)
            return 0;

        int cost = 0;
        for(int i = 0; i < selectedGrid.tower.GetComponentInChildren<TurretScript>().GetLevel(); ++i){
            cost += selectedGrid.tower.GetComponentInChildren<TurretScript>().GetCostArray()[i];
        }
        cost = (int)(cost * 3 / 4);
        Destroy(selectedGrid.tower);
        buildingPhaseSystem.SelectedWallButton();
        return cost;
    }
    public int DestroySelectedWall()
    {
        int cost = DestroySelectedTurret();
        Destroy(selectedGrid.wall);
        theGridRenderer.material.SetColor("_Color", Color.white);
        buildingPhaseSystem.SelectedNothingButton();
        return cost;
    }
    public int UpgradeSelectedTurret()
    {
       int cost = selectedGrid.tower.GetComponentInChildren<TurretScript>().GetCost();
       selectedGrid.tower.GetComponentInChildren<TurretScript>().LevelUp();
       UpdateRangeSphere();
       UpdateUpgradeCost();
       return cost;
    }
    public void UpdateUpgradeCost()
    {
        if (selectedGrid.tower == null)
            return;
        int currentLevel = selectedGrid.tower.GetComponentInChildren<TurretScript>().GetLevel();
        if (currentLevel > selectedGrid.tower.GetComponentInChildren<TurretScript>().MaxLevel - 1)
            return;
        buildingPhaseSystem.updateCost.text = selectedGrid.tower.GetComponentInChildren<TurretScript>().GetCostArray()[currentLevel].ToString();
        CheckTowerUpdate();
    }
    public void UpdateRangeSphere()
    {
        if (phaseMode == PHASE_MODE.LIST_OPEN && showcaseGO.CompareTag("Turret"))
        {
            rangeSphere = GameObject.Instantiate(rangeIndicator);
            rangeSphere.transform.position = showcaseGO.transform.position;
            rangeSphere.transform.SetParent(showcaseGO.transform);
            float tempScale = showcaseGO.GetComponentInChildren<TurretScript>().GetProximity() * 2;
            rangeSphere.transform.localScale = new Vector3(tempScale, tempScale, tempScale);
        }
        else if(phaseMode == PHASE_MODE.LIST_CLOSE && selectedGrid.tower != null)
        {
            if (rangeSphere == null)
            {
                rangeSphere = GameObject.Instantiate(rangeIndicator);
            }
            rangeSphere.transform.position = selectedGrid.tower.transform.position;
            rangeSphere.transform.SetParent(selectedGrid.tower.transform);
            float tempScale = selectedGrid.tower.GetComponentInChildren<TurretScript>().GetProximity() * 2;
            rangeSphere.transform.localScale = new Vector3(tempScale, tempScale, tempScale);
        }
    }
}
 