using UnityEngine;

public class GhostPreview : MonoBehaviour
{
    public void ShowAt(Vector3 position)
    {
        transform.position = position;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

}
