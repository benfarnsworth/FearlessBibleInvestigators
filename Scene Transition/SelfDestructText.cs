using UnityEngine;
using TMPro;

public class SelfDestructText : MonoBehaviour
{
    public float floatSpeed = 1.5f;
    public float lifeTime = 0.8f;

    private TextMeshPro textMesh;
    private Color originalColor;
    private float timer;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh != null) originalColor = textMesh.color;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime); // Fail-safe cleanup
    }

    /// <summary>
    /// Call this immediately after instantiating to set the text and optional color!
    /// </summary>
    public void SetText(string message, Color? textColor = null)
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshPro>();

        if (textMesh != null)
        {
            textMesh.text = message;
            if (textColor.HasValue)
            {
                textMesh.color = textColor.Value;
            }
            originalColor = textMesh.color;
        }
    }

    private void Update()
    {
        // Drift upwards
        transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);

        // Smoothly fade away over time
        timer += Time.deltaTime;
        if (textMesh != null)
        {
            Color newColor = Color.Lerp(originalColor, new Color(originalColor.r, originalColor.g, originalColor.b, 0f), timer / lifeTime);
            
            textMesh.color = newColor;
            textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
    }
}