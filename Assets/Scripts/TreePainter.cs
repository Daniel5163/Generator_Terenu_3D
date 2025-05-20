using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class TreePainter : MonoBehaviour
{
    [SerializeField] private List<GameObject> treePrefabs;  
    [SerializeField] private Button treeButton;            
    [SerializeField] private float treePlacementRate = 0.2f; 
    [SerializeField] private TerrainGenerator terrainGenerator;
    [SerializeField] private Celownik celownik;             

    private Camera playerCamera;
    private bool isPlacingTrees = false;                     
    private float treePlacementCooldown = 0f;                

    void Start()
    {
        playerCamera = Camera.main;

        if (treeButton != null)
        {
            treeButton.onClick.AddListener(ToggleTreeMode);   
            UpdateButtonColor();
        }

        if (terrainGenerator == null)
        {
            terrainGenerator = FindObjectOfType<TerrainGenerator>();
        }

        if (treePrefabs == null || treePrefabs.Count == 0)
        {
            Debug.LogError("Brak prefabów drzew!");
        }

        celownik.UpdatePosition(Vector3.zero, Quaternion.identity);
    }

    void Update()
    {
        if (isPlacingTrees)
        {
            UpdateBrushIndicator();

            // Sadzenie drzew gdy lewy przycisk myszy i nie na UI
            if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                PlaceTreeContinuous();
            }
        }
    }

    void UpdateBrushIndicator()
    {
        if (!isPlacingTrees) return;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.GetComponent<Terrain>() != null)
            {
                Vector3 position = hit.point;
                position.y = Terrain.activeTerrain.SampleHeight(position) + Terrain.activeTerrain.transform.position.y;
                celownik.UpdatePosition(position, Quaternion.identity); 
            }
        }
    }

    void PlaceTreeContinuous()
    {
       
        if (treePlacementCooldown > 0f && !Input.GetMouseButtonDown(0))
        {
            treePlacementCooldown -= Time.deltaTime;
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.GetComponent<Terrain>() != null)
            {
                Vector3 center = hit.point;
                float radius = celownik.BrushSize;

                int treeCount = Mathf.RoundToInt(radius * radius * 0.3f);
                float minDistance = 2f;

                List<Vector3> placedPositions = new List<Vector3>();

                for (int i = 0; i < treeCount; i++)
                {
                    int attempt = 0;
                    Vector3 spawnPosition;

                    do
                    {
                        Vector2 randomOffset = Random.insideUnitCircle * radius;
                        spawnPosition = new Vector3(
                            center.x + randomOffset.x,
                            0f,
                            center.z + randomOffset.y
                        );
                        spawnPosition.y = Terrain.activeTerrain.SampleHeight(spawnPosition) + Terrain.activeTerrain.transform.position.y;
                        attempt++;
                    }
                    while ((placedPositions.Exists(pos => Vector3.Distance(pos, spawnPosition) < minDistance) ||
                            !IsPositionOnTerrain(spawnPosition)) && attempt < 30);

                    if (attempt < 30)
                    {
                        GameObject randomTreePrefab = treePrefabs[Random.Range(0, treePrefabs.Count)];
                        GameObject newTree = Instantiate(
                            randomTreePrefab,
                            spawnPosition,
                            Quaternion.Euler(0, Random.Range(0f, 360f), 0)
                        );

                        terrainGenerator?.AddManualTree(newTree);  
                        placedPositions.Add(spawnPosition);
                    }
                }

                treePlacementCooldown = treePlacementRate; // Reset cooldownu
            }
        }
    }

    bool IsPositionOnTerrain(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return false;

        Vector3 terrainPos = terrain.transform.position;
        float terrainWidth = terrain.terrainData.size.x;
        float terrainLength = terrain.terrainData.size.z;

        // Sprawdzenie, czy pozycja mieści się w granicach terenu
        return position.x >= terrainPos.x && position.x <= terrainPos.x + terrainWidth &&
               position.z >= terrainPos.z && position.z <= terrainPos.z + terrainLength;
    }

    void ToggleTreeMode()
    {
        isPlacingTrees = !isPlacingTrees;
        UpdateButtonColor();

        // Włączenie lub wyłączenie generatora terenu podczas trybu sadzenia
        if (terrainGenerator != null)
        {
            terrainGenerator.enabled = !isPlacingTrees;
        }

        if (isPlacingTrees)
        {
            celownik.UpdatePosition(Vector3.zero, Quaternion.identity);
        }
        else
        {
            celownik.HideBrush();
        }
    }

    void UpdateButtonColor()
    {
        if (treeButton != null)
        {
            ColorBlock colors = treeButton.colors;
            colors.normalColor = isPlacingTrees ? Color.green : Color.white;
            colors.highlightedColor = isPlacingTrees ? new Color(0, 0.8f, 0) : new Color(0.9f, 0.9f, 0.9f);
            colors.pressedColor = isPlacingTrees ? new Color(0, 0.6f, 0) : new Color(0.7f, 0.7f, 0.7f);
            colors.selectedColor = isPlacingTrees ? Color.green : Color.white;
            treeButton.colors = colors;
        }
    }
}
