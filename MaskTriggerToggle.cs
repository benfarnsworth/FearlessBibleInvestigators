using UnityEngine;

public class MaskTriggerToggle : MonoBehaviour
{
    [SerializeField] private GameObject canopyMaskObject;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<ItemHolder>() != null)
        {
            var mask = other.transform.Find("CanopyMask");
            if (mask != null) mask.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<ItemHolder>() != null)
        {
            var mask = other.transform.Find("CanopyMask");
            if (mask != null) mask.gameObject.SetActive(false);
        }
    }
}