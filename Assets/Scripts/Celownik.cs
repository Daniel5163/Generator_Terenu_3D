using UnityEngine;

public class Celownik : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Color brushColor = new Color(0f, 0f, 1f, 0.3f);

    [Header("Size Settings")]
    [SerializeField] private float initialSize = 5f;
    [SerializeField] private float minSize = 1f;
    [SerializeField] private float maxSize = 20f;
    [SerializeField] private float sizeChangeSpeed = 1f;

    private GameObject brushIndicator;   
    private Material brushMaterial;      
    private float currentSize;           

    public float BrushSize => currentSize;

    void Start()
    {
        currentSize = initialSize;
        InitializeBrush();  // tworzymy i ustawiamy pêdzel
    }

    void Update()
    {
        HandleSizeInput();    // zmieniamy rozmiar na scrollu
    }

    void InitializeBrush()
    {
        brushIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(brushIndicator.GetComponent<Collider>());  

        brushMaterial = new Material(Shader.Find("Transparent/Diffuse"));
        brushMaterial.color = brushColor;
        brushIndicator.GetComponent<MeshRenderer>().material = brushMaterial;

        UpdateVisuals();   //skalê pêdzla
        brushIndicator.SetActive(true);
    }

    void HandleSizeInput()
    {
        float scrollDelta = Input.mouseScrollDelta.y;
        if (scrollDelta != 0)
        {
            currentSize = Mathf.Clamp(currentSize + scrollDelta * sizeChangeSpeed, minSize, maxSize);
            UpdateVisuals();  // odœwie¿amy wizualizacjê
        }
    }

    void UpdateVisuals()
    {
        if (brushIndicator != null)
        {
            brushIndicator.transform.localScale = new Vector3(currentSize * 2, 0.1f, currentSize * 2);
        }
    }

    public void UpdatePosition(Vector3 position, Quaternion rotation)
    {
        if (brushIndicator != null)
        {
            brushIndicator.SetActive(true);
            brushIndicator.transform.position = position;
            brushIndicator.transform.rotation = rotation;
        }
    }

    public void HideBrush()
    {
        if (brushIndicator != null)
            brushIndicator.SetActive(false);
    }

    void OnDestroy()
    {
        if (brushMaterial != null) Destroy(brushMaterial);
        if (brushIndicator != null) Destroy(brushIndicator);
    }
}
