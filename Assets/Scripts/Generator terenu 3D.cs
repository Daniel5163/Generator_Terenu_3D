using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    private Terrain terrain;
    private TerrainData terrainData;
    private Camera mainCamera;
    [SerializeField] private Celownik celownik; 

    private float brushStrength = 0.005f;   
    private float groundLevel = 0.5f;       
    private float valleyLevel = 0.3f;       
    private float mountainLevel = 0.7f;     

    private List<GameObject> manualTrees = new List<GameObject>(); 

    void Start()
    {
        terrain = GetComponent<Terrain>();
        terrainData = terrain.terrainData;
        mainCamera = Camera.main;

        if (terrainData == null)
        {
            // Inicjalizacja podstawowych parametrów terenu jeśli nie ma danych
            terrainData = new TerrainData();
            terrain.terrainData = terrainData;
            terrainData.size = new Vector3(200, 50, 200);
            terrainData.heightmapResolution = 513;
            terrainData.alphamapResolution = 512;
        }

        InitializeTerrainLayers(); // warstwy tekstur
        GenerateFlatTerrain();     // płaski teren na start
        ApplyTextureMap();         // nałóżenie tekstury zgodnie z wysokością

        celownik.UpdatePosition(Vector3.zero, Quaternion.identity); 
    }

    void InitializeTerrainLayers()
    {
        TerrainLayer[] terrainLayers = new TerrainLayer[3];

        // Warstwa wody (niebieska)
        terrainLayers[0] = new TerrainLayer();
        terrainLayers[0].diffuseTexture = CreateSolidTexture(Color.blue);
        terrainLayers[0].tileSize = new Vector2(50, 50);
        terrainLayers[0].name = "Water";

        // Warstwa trawy (zielona)
        terrainLayers[1] = new TerrainLayer();
        terrainLayers[1].diffuseTexture = CreateSolidTexture(Color.green);
        terrainLayers[1].tileSize = new Vector2(50, 50);
        terrainLayers[1].name = "Grass";

        // Warstwa gór (biała)
        terrainLayers[2] = new TerrainLayer();
        terrainLayers[2].diffuseTexture = CreateSolidTexture(Color.white);
        terrainLayers[2].tileSize = new Vector2(50, 50);
        terrainLayers[2].name = "Mountains";

        terrainData.terrainLayers = terrainLayers;
    }

    Texture2D CreateSolidTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    void Update()
    {
        UpdateBrushPosition();

        if (Input.GetMouseButton(0)) // LPM - podnoszenie terenu
        {
            ModifyTerrain(true);
        }
        else if (Input.GetMouseButton(1)) // PPM - obniżanie terenu
        {
            ModifyTerrain(false);
        }
    }

    void UpdateBrushPosition()
    {
        // Ustawienie pozycji pędzla na podstawie raycasta z kamery
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            celownik.UpdatePosition(
                hit.point + Vector3.up * 0.1f, 
                Quaternion.Euler(90f, 0f, 0f)
            );
        }
    }

    void ModifyTerrain(bool raise)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 hitPoint = hit.point;
            int resolution = terrainData.heightmapResolution;

            Vector3 terrainPos = terrain.transform.position;
            float xCoord = ((hitPoint.x - terrainPos.x) / terrainData.size.x) * resolution;
            float zCoord = ((hitPoint.z - terrainPos.z) / terrainData.size.z) * resolution;

            float[,] heights = terrainData.GetHeights(0, 0, resolution, resolution);
            List<Vector2Int> modifiedPixels = new List<Vector2Int>();

            float brushSize = celownik.BrushSize;

            // Modyfikacja wysokości terenu w obrębie pędzla
            for (int x = -Mathf.FloorToInt(brushSize); x <= Mathf.CeilToInt(brushSize); x++)
            {
                for (int z = -Mathf.FloorToInt(brushSize); z <= Mathf.CeilToInt(brushSize); z++)
                {
                    int coordX = Mathf.RoundToInt(xCoord) + x;
                    int coordZ = Mathf.RoundToInt(zCoord) + z;

                    if (coordX >= 0 && coordX < resolution && coordZ >= 0 && coordZ < resolution)
                    {
                        float distance = Mathf.Sqrt(x * x + z * z);
                        if (distance <= brushSize)
                        {
                            float influence = (brushSize - distance) / brushSize; 
                            float heightMod = brushStrength * influence * (raise ? 1 : -1);
                            heights[coordZ, coordX] = Mathf.Clamp01(heights[coordZ, coordX] + heightMod);
                            modifiedPixels.Add(new Vector2Int(coordX, coordZ));
                        }
                    }
                }
            }

            terrainData.SetHeights(0, 0, heights);
            UpdateTreePositions(modifiedPixels);
            ApplyTextureMap();                            
        }
    }

    void UpdateTreePositions(List<Vector2Int> modifiedPixels)
    {
        Vector3 terrainPos = terrain.transform.position;
        int resolution = terrainData.heightmapResolution;

        foreach (GameObject tree in manualTrees)
        {
            if (tree == null) continue;

            Vector3 treePos = tree.transform.position;
            int x = Mathf.RoundToInt((treePos.x - terrainPos.x) / terrainData.size.x * resolution);
            int z = Mathf.RoundToInt((treePos.z - terrainPos.z) / terrainData.size.z * resolution);

            // Jeśli drzewo jest blisko zmodyfikowanego fragmentu terenu, popraw jego wysokość
            foreach (Vector2Int pixel in modifiedPixels)
            {
                if (Mathf.Abs(pixel.x - x) <= 2 && Mathf.Abs(pixel.y - z) <= 2)
                {
                    float newHeight = terrainPos.y + terrainData.GetHeight(x, z);
                    tree.transform.position = new Vector3(
                        treePos.x,
                        newHeight,
                        treePos.z
                    );
                    break;
                }
            }
        }
    }

    void ApplyTextureMap()
    {
        int alphaMapResolution = terrainData.alphamapResolution;
        int numLayers = terrainData.terrainLayers.Length;
        float[,,] splatMap = new float[alphaMapResolution, alphaMapResolution, numLayers];

        int heightmapResolution = terrainData.heightmapResolution;
        float[,] heights = terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);

        for (int x = 0; x < alphaMapResolution; x++)
        {
            for (int y = 0; y < alphaMapResolution; y++)
            {
                int heightX = (int)((float)x / alphaMapResolution * heightmapResolution);
                int heightY = (int)((float)y / alphaMapResolution * heightmapResolution);

                heightX = Mathf.Clamp(heightX, 0, heightmapResolution - 1);
                heightY = Mathf.Clamp(heightY, 0, heightmapResolution - 1);

                float height = heights[heightY, heightX];

                for (int i = 0; i < numLayers; i++)
                {
                    splatMap[y, x, i] = 0f;
                }

                // Nakładanie tekstur na podstawie wysokości terenu
                if (height < valleyLevel)
                {
                    splatMap[y, x, 0] = 1f; 
                }
                else if (height < mountainLevel)
                {
                    splatMap[y, x, 1] = 1f; 
                }
                else
                {
                    splatMap[y, x, 2] = 1f; 
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, splatMap);
    }

    void GenerateFlatTerrain()
    {
        int resolution = terrainData.heightmapResolution;
        float[,] heights = new float[resolution, resolution];

        // Ustawienie płaskiego terenu na stały poziom
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                heights[x, y] = groundLevel;
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }

    public void AddManualTree(GameObject tree)
    {
        manualTrees.Add(tree); // Dodanie drzewa, które trzeba kontrolować po zmianach terenu
    }
}
