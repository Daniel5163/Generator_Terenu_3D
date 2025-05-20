using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float rotateSpeed = 2f;
    public float verticalRotationLimit = 80f;

    private float rotationY = 0f;
    private float rotationX = 0f;

    void Update()
    {
        // Ruch kamery wzglêdem osi
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 moveDirection = (transform.forward * moveZ + transform.right * moveX).normalized;
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // Obrót kamery na podstawie ruchu myszy
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        rotationY += mouseX * rotateSpeed;
        rotationX -= mouseY * rotateSpeed;
        rotationX = Mathf.Clamp(rotationX, -verticalRotationLimit, verticalRotationLimit);
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }
}
