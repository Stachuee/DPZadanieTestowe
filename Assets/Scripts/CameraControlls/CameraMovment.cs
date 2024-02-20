using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovment : MonoBehaviour
{
    [SerializeField] float normalSpeed;
    [SerializeField] float zoomSpeed;

    [SerializeField] float moveEdgeSize;

    [SerializeField] Vector3 camMoveCenter;
    [SerializeField] Vector3 camMoveRange;

    [SerializeField] Transform mount;

    void Update()
    {
        float verticalSpeed;// = Input.GetAxis("Vertical");
        float horizontalSpeed;// = Input.GetAxis("Horizontal");

        Vector2 mousePos = Input.mousePosition;

        verticalSpeed = (mousePos.y > Screen.height * (1 - moveEdgeSize) ? 1 : (mousePos.y < Screen.height * moveEdgeSize ? -1 : 0));
        horizontalSpeed = (mousePos.x > Screen.width * (1 - moveEdgeSize) ? 1 : (mousePos.x < Screen.width * moveEdgeSize ? -1 : 0));


        Vector2 scrollSpeed = Input.mouseScrollDelta;

        mount.position += Vector3.forward * verticalSpeed * normalSpeed * Time.deltaTime;
        mount.position += Vector3.right * horizontalSpeed * normalSpeed * Time.deltaTime;
        mount.position += Vector3.up * scrollSpeed.y * zoomSpeed * Time.deltaTime;

        mount.position = new Vector3(Mathf.Clamp(mount.position.x, camMoveCenter.x - camMoveRange.x, camMoveCenter.x + camMoveRange.x),
            Mathf.Clamp(mount.position.y, camMoveCenter.y - camMoveRange.y, camMoveCenter.y + camMoveRange.y),
            Mathf.Clamp(mount.position.z, camMoveCenter.z - camMoveRange.z, camMoveCenter.z + camMoveRange.z)
            );

    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(camMoveCenter, camMoveRange* 2);
    }
}
