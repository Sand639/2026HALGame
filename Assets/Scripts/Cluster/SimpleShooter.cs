using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleShooter : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private float rayDistance = 100f;

    private Mouse mouse;

    private void Awake()
    {
        mouse = Mouse.current;
    }

    private void Update()
    {
        if (mouse == null)
        {
            mouse = Mouse.current;
            if (mouse == null) return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        Vector2 screenPos = mouse.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            ConnectedClusterDestruction cluster =
                hit.collider.GetComponentInParent<ConnectedClusterDestruction>();

            if (cluster != null)
            {
                cluster.DamageAt(hit.point);
            }
        }
    }
}