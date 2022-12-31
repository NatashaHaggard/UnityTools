
using UnityEngine;

public class DebugVertexGizmos : MonoBehaviour
{
    [Range(0, 1)]
    public float sphereSize = 0.1f;

    void OnDrawGizmos()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            return;
        }

        // Get the sprite from the sprite renderer
        Sprite sprite = spriteRenderer.sprite;
        if (sprite == null)
        {
            return;
        }

        // Get the vertices of the sprite
        Vector2[] vertices = sprite.vertices;

        foreach (Vector2 vertex in vertices)
        {
            // Convert the vertex position from local space to world space
            Vector3 worldVertex = transform.TransformPoint(vertex);

            // Need a way to draw these points in the game scene view
            // Draw a yellow sphere at the transform's position
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(worldVertex, sphereSize);
        }
    }
}